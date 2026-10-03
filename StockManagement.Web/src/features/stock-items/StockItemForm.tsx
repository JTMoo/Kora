import { useEffect, useState, type FormEvent } from "react";
import { api, type ApiFailure, type StockItem, type Supplier } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n, type TextKey } from "../../i18n";
import { useLoad } from "../../useLoad";

const fields: (keyof StockItem & TextKey)[] = ["code", "barcode", "name", "description", "location", "amount", "manufacturer", "purchasePrice", "purchaseExchangeRate", "additionalPurchaseCost", "factor", "price", "minimumStock"];
const numberFields = new Set<string>(["amount", "price", "factor", "purchasePrice", "purchaseExchangeRate", "additionalPurchaseCost", "minimumStock"]);
const empty: StockItem = { id: "", code: "", name: "", description: "", location: "", amount: 0, price: 0, manufacturer: "", factor: 0, purchasePrice: 0, purchaseExchangeRate: 0, additionalPurchaseCost: 0, minimumStock: 0, barcode: "" };

export function StockItemForm({ editing, onSaved, onCancel }: { editing?: StockItem; onSaved: (stockItem: StockItem) => void; onCancel: () => void })
{
	const { t } = useI18n();
	const { data: suppliers = [] } = useLoad(api.listSuppliers);
	const [stockItem, setStockItem] = useState<StockItem>(editing ?? empty);
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	useEffect(() => setStockItem(editing ?? empty), [editing]);

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = editing ? await api.updateStockItem(stockItem) : await api.createStockItem(stockItem);
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		if (!editing) setStockItem(empty);
		onSaved(result.value);
	}

	return (
		<form onSubmit={onSubmit}>
			<div className="form-grid">
				{fields.map(field => (
					<label key={field}>
						{t(field)}
						<input
							type={numberFields.has(field) ? "number" : "text"}
							value={stockItem[field]}
							required={field === "code" || field === "name"}
							disabled={(field === "code" && !!editing) || (field === "price" && stockItem.factor > 0)}
							onChange={event => setStockItem({ ...stockItem, [field]: numberFields.has(field) ? Number(event.target.value) : event.target.value })}
						/>
					</label>
				))}
				<label>
					{t("supplier")}
					<select
						value={stockItem.supplierId ?? ""}
						onChange={event => setStockItem({ ...stockItem, supplierId: event.target.value || undefined })}
					>
						<option value="">{t("noSupplier")}</option>
						{suppliers.map((supplier: Supplier) => <option key={supplier.id} value={supplier.id}>{supplier.name}</option>)}
					</select>
				</label>
			</div>
			<FailureMessage failure={failure} />
			<div className="form-actions">
				<button type="submit" disabled={busy}>{t(editing ? "save" : "createItem")}</button>
				<button type="button" onClick={onCancel} disabled={busy}>{t("cancel")}</button>
			</div>
		</form>
	);
}
