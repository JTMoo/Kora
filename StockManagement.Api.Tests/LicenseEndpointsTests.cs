using System.Buffers.Text;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Features.Auth;
using StockManagement.Api.Features.Licensing;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Api.Tests;


[TestClass]
public sealed class LicenseEndpointsTests
{
	private ApiFactory _factory;
	private HttpClient _client;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		_factory = new();
		_client = await _factory.CreateAuthenticatedClientAsync();
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
	}


	[TestMethod]
	public async Task GetLicense_FreshInstall_StartsTrial()
	{
		// Act
		var response = await _client.GetAsync("/api/license");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var status = await response.Content.ReadAsAsync<LicenseStatusResponse>();
		Assert.AreEqual(LicenseStatus.Trial, status.Status);
		Assert.AreEqual(10, status.DaysRemaining);
	}

	[TestMethod]
	public async Task GetLicense_YearlyPrice_IsTenTimesMonthly()
	{
		// Act
		var status = await (await _client.GetAsync("/api/license")).Content.ReadAsAsync<LicenseStatusResponse>();

		// Assert
		Assert.AreEqual(status.MonthlyPricePyg * 10, status.YearlyPricePyg);
	}

	[TestMethod]
	public async Task Activate_InvalidKey_ReturnsUnprocessable()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/license/activate", new ActivateLicenseRequest("not-a-real-key"));

		// Assert
		Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
	}

	[TestMethod]
	public async Task Activate_ValidKey_ReturnsActiveAndPersists()
	{
		// Arrange: needs its own factory - the public key must be configured before the host builds
		using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
		using var factory = new ApiFactory(new Dictionary<string, string?> { ["Licensing:PublicKey"] = Convert.ToBase64String(signingKey.ExportSubjectPublicKeyInfo()) });
		using var client = await factory.CreateAuthenticatedClientAsync();
		var key = SignKey(signingKey, "Acme S.A.", LicensePlan.Yearly, DateTime.UtcNow.AddYears(1));

		// Act
		var response = await client.PostAsJsonAsync("/api/license/activate", new ActivateLicenseRequest(key));

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var status = await response.Content.ReadAsAsync<LicenseStatusResponse>();
		Assert.AreEqual(LicenseStatus.Active, status.Status);
		Assert.AreEqual(LicensePlan.Yearly, status.Plan);

		var reread = await (await client.GetAsync("/api/license")).Content.ReadAsAsync<LicenseStatusResponse>();
		Assert.AreEqual(LicenseStatus.Active, reread.Status);
	}

	[TestMethod]
	public async Task Locked_BusinessEndpointReturns402_ButAuthAndHealthStillWork()
	{
		// Arrange: trial + grace long lapsed, no activation
		var provider = _factory.ScopedServices.GetRequiredService<ILicenseServiceProvider>();
		await provider.AddAsync(new LicenseState { TrialStartedAtUtc = DateTime.UtcNow.AddDays(-30) });

		// Act
		var customersResponse = await _client.GetAsync("/api/customers");
		var healthResponse = await _client.GetAsync("/health");
		var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.SeededAdminUsername, ApiFactory.SeededAdminPassword));
		var licenseResponse = await _client.GetAsync("/api/license");

		// Assert
		Assert.AreEqual(HttpStatusCode.PaymentRequired, customersResponse.StatusCode);
		Assert.AreEqual("license_expired", (await JsonDocument.ParseAsync(await customersResponse.Content.ReadAsStreamAsync())).RootElement.GetProperty("code").GetString());
		Assert.AreEqual(HttpStatusCode.OK, healthResponse.StatusCode);
		Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);
		Assert.AreEqual(HttpStatusCode.OK, licenseResponse.StatusCode);
		Assert.AreEqual(LicenseStatus.Locked, (await licenseResponse.Content.ReadAsAsync<LicenseStatusResponse>()).Status);
	}

	[TestMethod]
	public async Task Locked_FeedbackEndpointStillWorks()
	{
		// Arrange: trial + grace long lapsed, no activation - a locked-out user must still be able to report it (ADR-0042)
		var provider = _factory.ScopedServices.GetRequiredService<ILicenseServiceProvider>();
		await provider.AddAsync(new LicenseState { TrialStartedAtUtc = DateTime.UtcNow.AddDays(-30) });

		// Act
		var response = await _client.PostAsJsonAsync("/api/feedback", new { Category = "Bug", Message = "stuck", LogExcerpt = (string?)null, CorrelationId = Guid.NewGuid().ToString() });

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
	}

	private static string SignKey(ECDsa signingKey, string licensee, LicensePlan plan, DateTime expiresAtUtc)
	{
		var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { Licensee = licensee, Plan = plan, IssuedAtUtc = DateTime.UtcNow, ExpiresAtUtc = expiresAtUtc });
		var signature = signingKey.SignData(payloadBytes, HashAlgorithmName.SHA256);
		return $"{Base64Url.EncodeToString(payloadBytes)}.{Base64Url.EncodeToString(signature)}";
	}
}
