using StockManagement.Kernel.Model;

namespace StockManagement.Sales.Core.Contracts;


public enum AddCashMovementError
{
	NoOpenSession,

	InvalidAmount
}


public sealed record AddCashMovementResult(bool Succeeded, CashMovement? Movement = null, AddCashMovementError? Error = null)
{
	public static AddCashMovementResult Success(CashMovement movement)
	{
		return new(true, movement);
	}

	public static AddCashMovementResult Failure(AddCashMovementError error)
	{
		return new(false, Error: error);
	}
}
