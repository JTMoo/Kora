import { StatusBadge, type StatusTone } from "../../StatusBadge";
import type { SupplierInvoice } from "../../api";
import type { TextKey } from "../../i18n";

const toneByStatus: Record<SupplierInvoice["status"], StatusTone> = { Open: "warning", PartiallyPaid: "warning", Paid: "success", Overdue: "danger" };
const labelKeyByStatus: Record<SupplierInvoice["status"], TextKey> = { Open: "statusPending", PartiallyPaid: "statusPending", Paid: "statusPaid", Overdue: "statusOverdue" };

export function supplierInvoiceStatusBadge(invoice: SupplierInvoice, t: (key: TextKey) => string)
{
	return <StatusBadge tone={toneByStatus[invoice.status]}>{t(labelKeyByStatus[invoice.status])}</StatusBadge>;
}
