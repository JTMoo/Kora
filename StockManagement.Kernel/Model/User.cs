using System.ComponentModel.DataAnnotations;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Model;


public class User : BaseDocument
{
	private string username = string.Empty;
	private string passwordHash = string.Empty;
	private string fullName = string.Empty;
	private string email = string.Empty;
	private string phone = string.Empty;
	private string position = string.Empty;
	private UserRole role = UserRole.Standard;
	private string[] permissions = [];
	private bool mustChangePassword;


	public string Username
	{
		get { return this.username; }
		set { this.SetField(ref this.username, value); }
	}

	/// <summary>
	/// PBKDF2-SHA256 hash, never the plain password
	/// </summary>
	public string PasswordHash
	{
		get { return this.passwordHash; }
		set { this.SetField(ref this.passwordHash, value); }
	}

	[Display(ResourceType = typeof(Language.Users), Name = nameof(Language.Users.fullName))]
	public string FullName
	{
		get { return this.fullName; }
		set { this.SetField(ref this.fullName, value); }
	}

	[Display(ResourceType = typeof(Language.Users), Name = nameof(Language.Users.email))]
	public string Email
	{
		get { return this.email; }
		set { this.SetField(ref this.email, value); }
	}

	[Display(ResourceType = typeof(Language.Users), Name = nameof(Language.Users.phone))]
	public string Phone
	{
		get { return this.phone; }
		set { this.SetField(ref this.phone, value); }
	}

	[Display(ResourceType = typeof(Language.Users), Name = nameof(Language.Users.position))]
	public string Position
	{
		get { return this.position; }
		set { this.SetField(ref this.position, value); }
	}

	[Display(ResourceType = typeof(Language.Users), Name = nameof(Language.Users.role))]
	public UserRole Role
	{
		get { return this.role; }
		set { this.SetField(ref this.role, value); }
	}

	/// <summary>
	/// Permission strings (see <c>StockManagement.Auth.Core.Contracts.Permission</c>) granted to a <see cref="UserRole.Standard"/> user; ignored for <see cref="UserRole.Admin"/>, who has every permission
	/// </summary>
	[Display(ResourceType = typeof(Language.Users), Name = nameof(Language.Users.rights))]
	public string[] Permissions
	{
		get { return this.permissions; }
		set { this.SetField(ref this.permissions, value); }
	}

	/// <summary>
	/// Set on the seeded admin account; cleared once that user changes their password. Blocks every other
	/// endpoint via <c>ForcePasswordChangeMiddleware</c> until cleared.
	/// </summary>
	public bool MustChangePassword
	{
		get { return this.mustChangePassword; }
		set { this.SetField(ref this.mustChangePassword, value); }
	}
}
