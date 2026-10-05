using StockManagement.Kernel.Model;

namespace StockManagement.Sales.Core.Contracts;


public enum OpenCashRegisterSessionError
{
	AlreadyOpen,

	InvalidOpeningFloat
}


public sealed record OpenCashRegisterSessionResult(bool Succeeded, CashRegisterSession? Session = null, OpenCashRegisterSessionError? Error = null)
{
	public static OpenCashRegisterSessionResult Success(CashRegisterSession session)
	{
		return new(true, session);
	}

	public static OpenCashRegisterSessionResult Failure(OpenCashRegisterSessionError error)
	{
		return new(false, Error: error);
	}
}
