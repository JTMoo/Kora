import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type StockItem, type Supplier } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";

const today = () => new Date().toISOString().slice(0, 10);

type PurchaseLine = { item: StockItem; amount: number; unitPrice: number };

export function SupplierInvoiceForm({ suppliers, onCreated }: { suppliers: Supplier[]; onCreated: () => void })
{
	const { t, formatNumber } = useI18n();
	const stockItems = useLoad(api.listStockItems);
	const [number, setNumber] = useState("");
	const [supplierId, setSupplierId] = useState("");
	const [date, setDate] = useState(today());
	const [expirationDate, setExpirationDate] = useState(today());
	const [total, setTotal] = useState("");
	const [code, setCode] = useState("");
	const [amount, setAmount] = useState(1);
	const [unitPrice, setUnitPrice] = useState("");
	const [lines, setLines] = useState<PurchaseLine[]>([]);
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	const computedTotal = lines.reduce((sum, line) => sum + line.amount * line.unitPrice, 0);

	function onAddLine()
	{
		const item = (stockItems.data ?? []).find(candidate => candidate.code === code);
		if (!item || amount < 1 || Number(unitPrice) < 0) return;

		setLines([...lines, { item, amount, unitPrice: Number(unitPrice) }]);
		setCode("");
		setAmount(1);
		setUnitPrice("");
	}

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.createSupplierInvoice({
			number,
			supplierId,
			date,
			expirationDate,
			total: lines.length > 0 ? computedTotal : Number(total),
			items: lines.length > 0 ? lines.map(line => ({ code: line.item.code, amount: line.amount, unitPrice: line.unitPrice })) : undefined
		});
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		setNumber("");
		setTotal("");
		setLines([]);
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
				{lines.length === 0 && (
					<label>
						{t("total")}
						<input type="number" min={0.01} step="0.01" required value={total} onChange={event => setTotal(event.target.value)} />
					</label>
				)}
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
				<label>
					{t("purchasePrice")}
					<input type="number" min={0} step="0.01" value={unitPrice} onChange={event => setUnitPrice(event.target.value)} />
				</label>
			</div>
			<div className="form-actions">
				<button type="button" onClick={onAddLine} disabled={!code}>{t("addToShoppingCart")}</button>
			</div>

			{lines.length > 0 && (
				<table aria-label={t("stockItems")}>
					<thead>
						<tr><th>{t("name")}</th><th className="number">{t("quantity")}</th><th className="number">{t("purchasePrice")}</th><th /></tr>
					</thead>
					<tbody>
						{lines.map(line => (
							<tr key={line.item.code}>
								<td>{line.item.name}<small>{line.item.code}</small></td>
								<td className="number">{formatNumber(line.amount)}</td>
								<td className="number">{formatNumber(line.amount * line.unitPrice)}</td>
								<td className="number"><button type="button" className="quiet" onClick={() => setLines(lines.filter(other => other !== line))}>{t("remove")}</button></td>
							</tr>
						))}
					</tbody>
					<tfoot>
						<tr className="total"><th>{t("total")}</th><td colSpan={2} className="number">{formatNumber(computedTotal)}</td><td /></tr>
					</tfoot>
				</table>
			)}

			<FailureMessage failure={failure} duplicate="supplierInvoiceNumberAlreadyExists" />
			<div className="form-actions">
				<button type="submit" disabled={busy}>{t("createSupplierInvoice")}</button>
			</div>
		</form>
	);
}
