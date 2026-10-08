import { StatusBadge, type StatusTone } from "../../StatusBadge";
import type { DebitNote } from "../../api";
import type { TextKey } from "../../i18n";

const toneByStatus: Record<DebitNote["transmissionStatus"], StatusTone> = { Pending: "warning", Sent: "info", Accepted: "success", Rejected: "danger", Error: "danger" };
const labelKeyByStatus: Record<DebitNote["transmissionStatus"], TextKey> = { Pending: "statusPending", Sent: "statusSent", Accepted: "statusAccepted", Rejected: "statusRejected", Error: "statusError" };

export function transmissionStatusBadge(debitNote: DebitNote, t: (key: TextKey) => string)
{
	return <StatusBadge tone={toneByStatus[debitNote.transmissionStatus]}>{t(labelKeyByStatus[debitNote.transmissionStatus])}</StatusBadge>;
}
