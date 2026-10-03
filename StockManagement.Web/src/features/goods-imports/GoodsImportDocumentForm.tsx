import { useState, type FormEvent } from "react";
import { api, incoterms, type ApiFailure, type GoodsImportDocument, type Incoterm, type StockItem } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";

type Line = { item: StockItem; amount: number };

export function GoodsImportDocumentForm({ onSaved, onCancel }: { onSaved: (document: GoodsImportDocument) => void; onCancel: () => void })
{
	const { t } = useI18n();
	const suppliers = useLoad(api.listSuppliers);
	const stockItems = useLoad(api.listStockItems);
	const [proformaNumber, setProformaNumber] = useState("");
	const [supplierId, setSupplierId] = useState("");
	const [incoterm, setIncoterm] = useState<Incoterm>("Fob");
	const [brokerName, setBrokerName] = useState("");
	const [duaReference, setDuaReference] = useState("");
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
		const result = await api.createGoodsImportDocument({
			proformaNumber,
			supplierId,
			incoterm,
			brokerName,
			duaReference,
			items: lines.map(line => ({ code: line.item.code, amount: line.amount }))
		});
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		onSaved(result.value);
	}

	return (
		<form onSubmit={onSubmit} className="panel">
			<FailureMessage failure={suppliers.failure ?? stockItems.failure} />
			<div className="form-grid">
				<label>
					{t("proformaNumber")}
					<input type="text" required value={proformaNumber} onChange={event => setProformaNumber(event.target.value)} />
				</label>
				<label>
					{t("supplier")}
					<select value={supplierId} required onChange={event => setSupplierId(event.target.value)}>
						<option value="">{t("selectSupplier")}</option>
						{suppliers.data?.map(supplier => <option key={supplier.id} value={supplier.id}>{supplier.name}</option>)}
					</select>
				</label>
				<label>
					{t("incoterm")}
					<select value={incoterm} onChange={event => setIncoterm(event.target.value as Incoterm)}>
						{incoterms.map(value => <option key={value} value={value}>{value.toUpperCase()}</option>)}
					</select>
				</label>
				<label>
					{t("brokerName")}
					<input type="text" required value={brokerName} onChange={event => setBrokerName(event.target.value)} />
				</label>
				<label>
					{t("duaReference")}
					<input type="text" required value={duaReference} onChange={event => setDuaReference(event.target.value)} />
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
			<table aria-label={t("goodsImportDocument")}>
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
			<FailureMessage failure={failure} notFound="supplierNotFound" duplicate="proformaNumberAlreadyExists" />
			<div className="form-actions">
				<button type="submit" className="primary" disabled={busy || lines.length === 0 || !supplierId || !proformaNumber || !brokerName || !duaReference}>{t("createGoodsImportDocument")}</button>
				<button type="button" onClick={onCancel}>{t("cancel")}</button>
			</div>
		</form>
	);
}
