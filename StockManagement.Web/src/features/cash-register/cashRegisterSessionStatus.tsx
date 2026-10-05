import { StatusBadge, type StatusTone } from "../../StatusBadge";
import type { CashRegisterSession } from "../../api";
import type { TextKey } from "../../i18n";

const toneByStatus: Record<CashRegisterSession["status"], StatusTone> = { Open: "info", Closed: "muted" };
const labelKeyByStatus: Record<CashRegisterSession["status"], TextKey> = { Open: "statusOpen", Closed: "statusClosed" };

export function cashRegisterSessionStatusBadge(session: CashRegisterSession, t: (key: TextKey) => string)
{
	return <StatusBadge tone={toneByStatus[session.status]}>{t(labelKeyByStatus[session.status])}</StatusBadge>;
}
