import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type PaymentMethod } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n, type TextKey } from "../../i18n";

const methods: { value: PaymentMethod; label: TextKey }[] = [
	{ value: "Cash", label: "cash" },
	{ value: "BankTransfer", label: "bankTransfer" },
	{ value: "Check", label: "check" },
	{ value: "BancardQr", label: "bancardQr" },
	{ value: "Other", label: "other" }
];

export function RecordPaymentForm({ invoiceNumber, onPaid, onCancel }: { invoiceNumber: string; onPaid: () => void; onCancel: () => void })
{
	const { t } = useI18n();
	const [amount, setAmount] = useState("");
	const [method, setMethod] = useState<PaymentMethod>("Cash");
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.createSupplierPayment(invoiceNumber, { amount: Number(amount), method });
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		onPaid();
	}

	return (
		<form onSubmit={onSubmit} className="form-grid" aria-label={t("recordPayment")}>
			<label>
				{t("paymentAmount")}
				<input type="number" min={0.01} step="0.01" required value={amount} onChange={event => setAmount(event.target.value)} />
			</label>
			<label>
				{t("paymentMethod")}
				<select value={method} onChange={event => setMethod(event.target.value as PaymentMethod)}>
					{methods.map(option => <option key={option.value} value={option.value}>{t(option.label)}</option>)}
				</select>
			</label>
			<FailureMessage failure={failure} />
			<div className="form-actions">
				<button type="submit" disabled={busy}>{t("recordPayment")}</button>
				<button type="button" onClick={onCancel}>{t("cancel")}</button>
			</div>
		</form>
	);
}
