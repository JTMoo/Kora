using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Features.Users;


/// <summary>User without its password hash.</summary>
public sealed record UserResponse(string Id, string Username, string FullName, string Email, string Phone, string Position, UserRole Role, IReadOnlyList<string> Permissions, bool MustChangePassword)
{
	public static UserResponse From(User user)
	{
		return new(user.Id, user.Username, user.FullName, user.Email, user.Phone, user.Position, user.Role, user.Permissions, user.MustChangePassword);
	}
}
