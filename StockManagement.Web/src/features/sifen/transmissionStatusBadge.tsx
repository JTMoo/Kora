import { StatusBadge, type StatusTone } from "../../StatusBadge";
import type { SifenTransmissionStatus } from "../../api";
import type { TextKey } from "../../i18n";

const toneByStatus: Record<SifenTransmissionStatus, StatusTone> = { Pending: "warning", Sent: "info", Accepted: "success", Rejected: "danger", Error: "danger", Cancelled: "muted" };
const labelKeyByStatus: Record<SifenTransmissionStatus, TextKey> = { Pending: "statusPending", Sent: "statusSent", Accepted: "statusAccepted", Rejected: "statusRejected", Error: "statusError", Cancelled: "cancelled" };

export function transmissionStatusBadge(status: SifenTransmissionStatus, t: (key: TextKey) => string)
{
	return <StatusBadge tone={toneByStatus[status]}>{t(labelKeyByStatus[status])}</StatusBadge>;
}
