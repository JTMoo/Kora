import { StatusBadge, type StatusTone } from "../../StatusBadge";
import type { PaymentLink } from "../../api";
import type { TextKey } from "../../i18n";

const toneByStatus: Record<PaymentLink["status"], StatusTone> = { Pending: "warning", Paid: "success", Expired: "muted", Cancelled: "muted", Failed: "danger" };
const labelKeyByStatus: Record<PaymentLink["status"], TextKey> = { Pending: "statusPending", Paid: "statusPaid", Expired: "statusExpired", Cancelled: "cancelled", Failed: "statusFailed" };

export function paymentLinkStatusBadge(paymentLink: PaymentLink, t: (key: TextKey) => string)
{
	return <StatusBadge tone={toneByStatus[paymentLink.status]}>{t(labelKeyByStatus[paymentLink.status])}</StatusBadge>;
}
