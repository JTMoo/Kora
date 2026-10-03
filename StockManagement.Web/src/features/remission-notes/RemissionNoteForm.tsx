import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type RemissionNote, type RemissionReason, type StockItem } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";
import { reasons } from "./remissionNoteStatus";

type Line = { item: StockItem; amount: number };

export function RemissionNoteForm({ onSaved, onCancel }: { onSaved: (remissionNote: RemissionNote) => void; onCancel: () => void })
{
	const { t } = useI18n();
	const customers = useLoad(api.listCustomers);
	const stockItems = useLoad(api.listStockItems);
	const [customerId, setCustomerId] = useState("");
	const [reason, setReason] = useState<RemissionReason>("Venta");
	const [destinationAddress, setDestinationAddress] = useState("");
	const [code, setCode] = useState("");
	const [amount, setAmount] = useState(1);
	const [lines, setLines] = useState<Line[]>([]);
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	function onAddLine()
	{
		const item = (stockItems.data ?? []).find(candidate => candidate.code === code);
		if (!item || amount < 1) return;

		const existing = lines.find(line => line.item.code === item.code);
		setLines(existing
			? lines.map(line => line === existing ? { ...line, amount: line.amount + amount } : line)
			: [...lines, { item, amount }]);
		setCode("");
		setAmount(1);
	}

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.createRemissionNote({
			customerId: Number(customerId),
			reason,
			destinationAddress,
			items: lines.map(line => ({ code: line.item.code, amount: line.amount }))
		});
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		onSaved(result.value);
	}

	return (
		<form onSubmit={onSubmit} className="panel">
			<FailureMessage failure={customers.failure ?? stockItems.failure} />
			<div className="form-grid">
				<label>
					{t("customer")}
					<select value={customerId} required onChange={event => setCustomerId(event.target.value)}>
						<option value="">{t("selectCustomer")}</option>
						{customers.data?.map(customer => <option key={customer.customerId} value={customer.customerId}>{`${customer.customerId} ${customer.name} ${customer.lastname}`.trim()}</option>)}
					</select>
				</label>
				<label>
					{t("reason")}
					<select value={reason} onChange={event => setReason(event.target.value as RemissionReason)}>
						{reasons.map(option => <option key={option.value} value={option.value}>{t(option.key)}</option>)}
					</select>
				</label>
				<label>
					{t("destinationAddress")}
					<input type="text" required value={destinationAddress} onChange={event => setDestinationAddress(event.target.value)} />
				</label>
			</div>
			<div className="form-grid">
				<label>
					{t("stockItem")}
					<select value={code} onChange={event => setCode(event.target.value)}>
						<option value="" />
						{(stockItems.data ?? []).map(item => <option key={item.code} value={item.code}>{`${item.code} ${item.name}`}</option>)}
					</select>
				</label>
				<label>
					{t("quantity")}
					<input type="number" min={1} value={amount} onChange={event => setAmount(Number(event.target.value))} />
				</label>
			</div>
			<div className="form-actions">
				<button type="button" onClick={onAddLine} disabled={!code}>{t("addToShoppingCart")}</button>
			</div>
			<table aria-label={t("remissionNote")}>
				<thead>
					<tr><th>{t("code")}</th><th>{t("name")}</th><th className="number">{t("quantity")}</th><th /></tr>
				</thead>
				<tbody>
					{lines.map(line => (
						<tr key={line.item.code}>
							<td>{line.item.code}</td><td>{line.item.name}</td><td className="number">{line.amount}</td>
							<td className="number"><button type="button" className="quiet" onClick={() => setLines(lines.filter(other => other !== line))}>{t("remove")}</button></td>
						</tr>
					))}
				</tbody>
			</table>
			<FailureMessage failure={failure} notFound="customerNotFound" />
			<div className="form-actions">
				<button type="submit" className="primary" disabled={busy || lines.length === 0 || !customerId || !destinationAddress}>{t("createRemissionNote")}</button>
				<button type="button" onClick={onCancel}>{t("cancel")}</button>
			</div>
		</form>
	);
}
