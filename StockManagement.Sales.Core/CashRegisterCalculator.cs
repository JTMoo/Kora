using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Sales.Core;


/// <summary>
/// Expected till cash for a <see cref="CashRegisterSession"/>, derived from its movements and cash payments; never stored.
/// </summary>
internal static class CashRegisterCalculator
{
	/// <param name="cashPaymentsTotal">Sum of sale payments tagged with the session (see <see cref="Kernel.Database.Interfaces.ICashRegisterSessionServiceProvider"/>)</param>
	public static decimal ExpectedAmount(CashRegisterSession session, decimal cashPaymentsTotal)
	{
		var cashIn = session.Movements.Where(movement => movement.Type == CashMovementType.In).Sum(movement => movement.Amount);
		var cashOut = session.Movements.Where(movement => movement.Type == CashMovementType.Out).Sum(movement => movement.Amount);
		return session.OpeningFloat + cashIn - cashOut + cashPaymentsTotal;
	}
}
