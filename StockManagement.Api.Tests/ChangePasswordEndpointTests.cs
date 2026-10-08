using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using StockManagement.Api.Features.Auth;
using StockManagement.Api.Features.Users;

namespace StockManagement.Api.Tests;


[TestClass]
public sealed class ChangePasswordEndpointTests
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
	public async Task ChangePassword_CorrectCurrentPassword_LogsInWithNewOne()
	{
		// Act
		var response = await _client.PutAsJsonAsync("/api/auth/password", new ChangePasswordRequest(ApiFactory.SeededAdminPassword, "newPassword1!"));

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var login = await (await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.SeededAdminUsername, "newPassword1!"))).Content.ReadAsAsync<LoginResponse>();
		Assert.IsFalse(login.MustChangePassword);
	}

	[TestMethod]
	public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
	{
		// Act
		var response = await _client.PutAsJsonAsync("/api/auth/password", new ChangePasswordRequest("WrongPassword1!", "newPassword1!"));

		// Assert: 400, not 401 - a 401 here would trip the web client's shared "session expired" handler
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), ChangePasswordEndpoint.IncorrectCurrentPassword);
	}

	[TestMethod]
	public async Task ChangePassword_ShortNewPassword_ReturnsBadRequest()
	{
		// Act
		var response = await _client.PutAsJsonAsync("/api/auth/password", new ChangePasswordRequest(ApiFactory.SeededAdminPassword, "short"));

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "passwordTooShort");
	}

	[TestMethod]
	public async Task ChangePassword_ClearsMustChangePasswordOnOwnUser()
	{
		// Act
		await _client.PutAsJsonAsync("/api/auth/password", new ChangePasswordRequest(ApiFactory.SeededAdminPassword, "newPassword1!"));

		// Assert
		var admin = (await _client.GetFromJsonAsync<UserListResponse>("/api/users", ApiFactory.JsonOptions)).Items.Single(user => user.Username == ApiFactory.SeededAdminUsername);
		Assert.IsFalse(admin.MustChangePassword);
	}
}


/// <summary>
/// Verifies <c>ForcePasswordChangeMiddleware</c> directly (unlike every other test here, logs in without going
/// through <see cref="ApiFactory.CreateAuthenticatedClientAsync()"/>'s flag-clearing, since that's the behavior
/// under test).
/// </summary>
[TestClass]
public sealed class ForcePasswordChangeMiddlewareTests
{
	private ApiFactory _factory;
	private HttpClient _client;


	[TestInitialize]
	public void Initialize()
	{
		_factory = new();
		_client = _factory.CreateClient();
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
	}


	[TestMethod]
	public async Task SeededAdmin_BeforeChangingPassword_IsBlockedFromOtherEndpoints()
	{
		// Arrange
		var login = await (await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.SeededAdminUsername, ApiFactory.SeededAdminPassword))).Content.ReadAsAsync<LoginResponse>();
		Assert.IsTrue(login.MustChangePassword);
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

		// Act
		var response = await _client.GetAsync("/api/users");

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "password_change_required");
	}

	[TestMethod]
	public async Task SeededAdmin_ChangePasswordEndpointStaysReachableWhileBlocked()
	{
		// Arrange
		var login = await (await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.SeededAdminUsername, ApiFactory.SeededAdminPassword))).Content.ReadAsAsync<LoginResponse>();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

		// Act
		var response = await _client.PutAsJsonAsync("/api/auth/password", new ChangePasswordRequest(ApiFactory.SeededAdminPassword, "newPassword1!"));

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
	}

	[TestMethod]
	public async Task SeededAdmin_AfterChangingPassword_IsNoLongerBlocked()
	{
		// Arrange
		var login = await (await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.SeededAdminUsername, ApiFactory.SeededAdminPassword))).Content.ReadAsAsync<LoginResponse>();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
		await _client.PutAsJsonAsync("/api/auth/password", new ChangePasswordRequest(ApiFactory.SeededAdminPassword, "newPassword1!"));

		// Act
		var response = await _client.GetAsync("/api/users");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
	}
}
