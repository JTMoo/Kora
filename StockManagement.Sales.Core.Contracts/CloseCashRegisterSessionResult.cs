using StockManagement.Kernel.Model;

namespace StockManagement.Sales.Core.Contracts;


public enum CloseCashRegisterSessionError
{
	NoOpenSession,

	NegativeCountedAmount
}


/// <param name="ExpectedAmount">Opening float plus cash in, minus cash out, plus cash sale payments recorded during the session</param>
/// <param name="Difference"><see cref="CashRegisterSession.CountedAmount"/> minus <paramref name="ExpectedAmount"/>; negative is a shortfall</param>
public sealed record CashRegisterCloseReport(CashRegisterSession Session, decimal ExpectedAmount, decimal CountedAmount, decimal Difference);


public sealed record CloseCashRegisterSessionResult(bool Succeeded, CashRegisterCloseReport? Report = null, CloseCashRegisterSessionError? Error = null)
{
	public static CloseCashRegisterSessionResult Success(CashRegisterCloseReport report)
	{
		return new(true, report);
	}

	public static CloseCashRegisterSessionResult Failure(CloseCashRegisterSessionError error)
	{
		return new(false, Error: error);
	}
}
