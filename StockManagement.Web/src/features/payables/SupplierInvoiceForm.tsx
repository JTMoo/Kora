import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type Supplier } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";

const today = () => new Date().toISOString().slice(0, 10);

export function SupplierInvoiceForm({ suppliers, onCreated }: { suppliers: Supplier[]; onCreated: () => void })
{
	const { t } = useI18n();
	const [number, setNumber] = useState("");
	const [supplierId, setSupplierId] = useState("");
	const [date, setDate] = useState(today());
	const [expirationDate, setExpirationDate] = useState(today());
	const [total, setTotal] = useState("");
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.createSupplierInvoice({ number, supplierId, date, expirationDate, total: Number(total) });
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		setNumber("");
		setTotal("");
		onCreated();
	}

	return (
		<form onSubmit={onSubmit} className="panel">
			<div className="form-grid">
				<label>
					{t("supplierInvoiceNumber")}
					<input type="text" required value={number} onChange={event => setNumber(event.target.value)} />
				</label>
				<label>
					{t("supplier")}
					<select required value={supplierId} onChange={event => setSupplierId(event.target.value)}>
						<option value="" disabled>{t("supplier")}</option>
						{suppliers.map(supplier => <option key={supplier.id} value={supplier.id}>{supplier.name}</option>)}
					</select>
				</label>
				<label>
					{t("creationDate")}
					<input type="date" required value={date} onChange={event => setDate(event.target.value)} />
				</label>
				<label>
					{t("expirationDate")}
					<input type="date" required value={expirationDate} onChange={event => setExpirationDate(event.target.value)} />
				</label>
				<label>
					{t("total")}
					<input type="number" min={0.01} step="0.01" required value={total} onChange={event => setTotal(event.target.value)} />
				</label>
			</div>
			<FailureMessage failure={failure} duplicate="supplierInvoiceNumberAlreadyExists" />
			<div className="form-actions">
				<button type="submit" disabled={busy}>{t("createSupplierInvoice")}</button>
			</div>
		</form>
	);
}
