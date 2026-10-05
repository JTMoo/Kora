using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Features.CashRegister;


public sealed record CashRegisterSessionResponse(
	string Id,
	DateTime OpenedAt,
	string OpenedByUserId,
	decimal OpeningFloat,
	DateTime? ClosedAt,
	string? ClosedByUserId,
	decimal? CountedAmount,
	string Note,
	CashRegisterSessionStatus Status,
	decimal ExpectedAmount,
	IReadOnlyList<CashMovementResponse> Movements)
{
	public static CashRegisterSessionResponse From(CashRegisterSession session, decimal expectedAmount)
	{
		return new(
			session.Id,
			session.OpenedAt,
			session.OpenedByUserId,
			session.OpeningFloat,
			session.ClosedAt,
			session.ClosedByUserId,
			session.CountedAmount,
			session.Note,
			session.Status,
			expectedAmount,
			session.Movements.Select(CashMovementResponse.From).ToList());
	}
}
