using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Features.CashRegister;


public sealed record CashMovementResponse(CashMovementType Type, decimal Amount, string Reason, DateTime Date, string CreatedByUserId)
{
	public static CashMovementResponse From(CashMovement movement)
	{
		return new(movement.Type, movement.Amount, movement.Reason, movement.Date, movement.CreatedByUserId);
	}
}
