import { StatusBadge, type StatusTone } from "../../StatusBadge";
import type { RemissionNote, RemissionReason } from "../../api";
import type { TextKey } from "../../i18n";

export const reasons: { value: RemissionReason; key: TextKey }[] = [
	{ value: "Venta", key: "reasonVenta" },
	{ value: "Consignacion", key: "reasonConsignacion" },
	{ value: "Reposicion", key: "reasonReposicion" },
	{ value: "TrasladoEntreLocales", key: "reasonTrasladoEntreLocales" },
	{ value: "Otro", key: "reasonOtro" }
];

const reasonKeys: Record<RemissionReason, TextKey> = Object.fromEntries(reasons.map(reason => [reason.value, reason.key])) as Record<RemissionReason, TextKey>;

const toneByStatus: Record<RemissionNote["transmissionStatus"], StatusTone> = { Pending: "warning", Sent: "info", Accepted: "success", Rejected: "danger", Error: "danger" };
const labelKeyByStatus: Record<RemissionNote["transmissionStatus"], TextKey> = { Pending: "statusPending", Sent: "statusSent", Accepted: "statusAccepted", Rejected: "statusRejected", Error: "statusError" };

export function reasonLabel(reason: RemissionReason, t: (key: TextKey) => string)
{
	return t(reasonKeys[reason]);
}

export function transmissionStatusBadge(remissionNote: RemissionNote, t: (key: TextKey) => string)
{
	return <StatusBadge tone={toneByStatus[remissionNote.transmissionStatus]}>{t(labelKeyByStatus[remissionNote.transmissionStatus])}</StatusBadge>;
}
