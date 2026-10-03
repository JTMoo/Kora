import { useState } from "react";
import { api, type StockItem } from "../../api";
import { ConfirmAction } from "../../ConfirmAction";
import { Dialog } from "../../Dialog";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { StatusBadge, type StatusTone } from "../../StatusBadge";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";
import { useToast } from "../../Toast";
import { OpeningStockImport } from "./OpeningStockImport";
import { StockCheckForm } from "./StockCheckForm";
import { StockItemForm } from "./StockItemForm";
import { StockItemImport } from "./StockItemImport";

export function StockItemList()
{
	const { t, formatNumber } = useI18n();
	const { show: showToast } = useToast();
	const { data: stockItems = [], setData, failure } = useLoad(api.listStockItems);
	const [search, setSearch] = useState("");
	const [editing, setEditing] = useState<StockItem>();
	const [formOpen, setFormOpen] = useState(false);
	const [checking, setChecking] = useState<StockItem>();
	const [showImport, setShowImport] = useState(false);
	const [showOpeningStock, setShowOpeningStock] = useState(false);
	const [belowMinimumOnly, setBelowMinimumOnly] = useState(false);

	function onAddNew()
	{
		setEditing(undefined);
		setFormOpen(true);
	}

	function onEdit(item: StockItem)
	{
		setEditing(item);
		setFormOpen(true);
	}

	function onFormCancel()
	{
		setFormOpen(false);
		setEditing(undefined);
	}

	async function onImported()
	{
		const result = await api.listStockItems();
		if (result.ok) setData(result.value);
	}

	const term = search.trim().toLowerCase();
	const visible = stockItems
		.filter(item => [item.code, item.barcode, item.name, item.description, item.location].some(value => value.toLowerCase().includes(term)))
		.filter(item => !belowMinimumOnly || (item.minimumStock > 0 && item.amount < item.minimumStock));

	function onSaved(stockItem: StockItem)
	{
		setData(editing ? stockItems.map(item => item.id === editing.id ? stockItem : item) : [...stockItems, stockItem]);
		setEditing(undefined);
		setFormOpen(false);
		showToast(t("savedToast").replace("{0}", t("stockItem")));
	}

	function onChecked(stockItem: StockItem)
	{
		setData(stockItems.map(item => item.id === stockItem.id ? stockItem : item));
		setChecking(undefined);
	}

	async function onDelete(stockItem: StockItem)
	{
		const result = await api.deleteStockItem(stockItem);
		if (!result.ok) return;

		setData(stockItems.filter(item => item.id !== stockItem.id));
		if (editing?.id === stockItem.id) onFormCancel();
		if (checking?.id === stockItem.id) setChecking(undefined);
	}

	return (
		<Page title={t("stockItems")} toolbar={<>
			<input type="search" aria-label={t("search")} placeholder={t("searchBoxDefault")} value={search} onChange={event => setSearch(event.target.value)} />
			<button type="button" className="quiet" aria-pressed={belowMinimumOnly} onClick={() => setBelowMinimumOnly(!belowMinimumOnly)}>{t("belowMinimum")}</button>
			<button type="button" className="quiet" aria-pressed={showImport} onClick={() => setShowImport(!showImport)}>{t("excelImport")}</button>
			<button type="button" className="quiet" aria-pressed={showOpeningStock} onClick={() => setShowOpeningStock(!showOpeningStock)}>{t("openingStock")}</button>
			<button type="button" className="primary" onClick={onAddNew}>+ {t("addNew")}</button>
		</>}>
			<Dialog open={formOpen} onClose={onFormCancel} title={t(editing ? "edit" : "addNew")}>
				<StockItemForm editing={editing} onSaved={onSaved} onCancel={onFormCancel} />
			</Dialog>
			{checking && <StockCheckForm stockItem={checking} onChecked={onChecked} onCancel={() => setChecking(undefined)} />}
			{showImport && <StockItemImport onImported={onImported} />}
			{showOpeningStock && <OpeningStockImport onImported={onImported} />}
			<FailureMessage failure={failure} />
			<table>
				<thead>
					<tr><th>{t("name")}</th><th>{t("code")}</th><th className="number">{t("quantity")}</th><th className="number">{t("price")}</th><th>{t("manufacturer")}</th><th>{t("location")}</th><th>{t("supplier")}</th><th>{t("status")}</th><th></th></tr>
				</thead>
				<tbody>
					{visible.map(item =>
					{
						const belowMinimum = item.minimumStock > 0 && item.amount < item.minimumStock;
						const status: { tone: StatusTone; label: string } = item.amount === 0
							? { tone: "danger", label: t("statusOutOfStock") }
							: belowMinimum ? { tone: "warning", label: t("statusLowStock") } : { tone: "success", label: t("statusInStock") };
						return (
						<tr key={item.code} className={belowMinimum ? "below-minimum" : item.amount === 0 ? "sold-out" : undefined}>
							<td>{item.name}</td><td>{item.code}</td>
							<td className="number">{formatNumber(item.amount)}</td><td className="number">{formatNumber(item.price)}</td>
							<td>{item.manufacturer}</td><td>{item.location}</td><td>{item.supplierName ?? ""}</td>
							<td><StatusBadge tone={status.tone}>{status.label}</StatusBadge></td>
							<td className="row-actions">
								<button type="button" className="quiet" onClick={() => onEdit(item)}>{t("edit")}</button>
								<button type="button" className="quiet" onClick={() => setChecking(item)}>{t("checkStock")}</button>
								<ConfirmAction label={t("deleteItem")} message={t("itemDeletionPrompt").replace("{0}", item.name)} onConfirm={() => onDelete(item)} />
							</td>
						</tr>
						);
					})}
				</tbody>
			</table>
		</Page>
	);
}
