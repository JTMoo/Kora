import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type Invoice, type SaleCondition, type StockItem } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";
import { useToast } from "../../Toast";

type CartLine = { item: StockItem; amount: number };

export function SaleForm({ onSold }: { onSold: (invoice: Invoice) => void })
{
	const { t, formatNumber } = useI18n();
	const { show: showToast } = useToast();
	const customers = useLoad(api.listCustomers);
	const stockItems = useLoad(api.listStockItems);
	const [customerId, setCustomerId] = useState("");
	const [code, setCode] = useState("");
	const [barcode, setBarcode] = useState("");
	const [amount, setAmount] = useState(1);
	const [saleCondition, setSaleCondition] = useState<SaleCondition>("Cash");
	const [cart, setCart] = useState<CartLine[]>([]);
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	const available = (stockItems.data ?? []).filter(item => item.amount > 0);
	const total = cart.reduce((sum, line) => sum + line.item.price * line.amount, 0);

	function addToCart(item: StockItem)
	{
		const existing = cart.find(line => line.item.code === item.code);
		setCart(existing
			? cart.map(line => line === existing ? { ...line, amount: line.amount + amount } : line)
			: [...cart, { item, amount }]);
		setAmount(1);
	}

	function onAdd()
	{
		const item = available.find(candidate => candidate.code === code);
		if (!item || amount < 1) return;
		addToCart(item);
	}

	async function onScan()
	{
		if (!barcode) return;

		const result = await api.getStockItemByBarcode(barcode);
		setBarcode("");
		if (!result.ok) return showToast(t("barcodeNotFound"));

		addToCart(result.value);
	}

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.createSale({ customerId: Number(customerId), saleCondition, items: cart.map(line => ({ code: line.item.code, amount: line.amount })) });
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		showToast(t("invoiceSavedToast").replace("{0}", result.value.number));
		onSold(result.value);
	}

	return (
		<Page title={t("newSale")}>
			<FailureMessage failure={customers.failure ?? stockItems.failure} />
			<form onSubmit={onSubmit} className="sale">
				<div className="panel">
					<div className="form-grid">
						<label>
							{t("customer")}
							<select value={customerId} required onChange={event => setCustomerId(event.target.value)}>
								<option value="">{t("selectCustomer")}</option>
								{customers.data?.map(customer => <option key={customer.customerId} value={customer.customerId}>{`${customer.customerId} ${customer.name} ${customer.lastname}`.trim()}</option>)}
							</select>
						</label>
						<fieldset className="segmented">
							<legend>{t("saleCondition")}</legend>
							<div>
								{(["Cash", "Credit"] as const).map(condition => (
									<label key={condition}>
										<input type="radio" name="saleCondition" value={condition} checked={saleCondition === condition} onChange={() => setSaleCondition(condition)} />
										{t(condition === "Cash" ? "cash" : "credit")}
									</label>
								))}
							</div>
						</fieldset>
						<label>
							{t("stockItem")}
							<select value={code} onChange={event => setCode(event.target.value)}>
								<option value="" />
								{available.map(item => <option key={item.code} value={item.code}>{`${item.code} ${item.name} (${item.amount})`}</option>)}
							</select>
						</label>
						<label>
							{t("quantity")}
							<input type="number" min={1} value={amount} onChange={event => setAmount(Number(event.target.value))} />
						</label>
					</div>
					<div className="form-actions">
						<button type="button" onClick={onAdd} disabled={!code}>{t("addToShoppingCart")}</button>
					</div>
					<div className="form-grid">
						<label>
							{t("scanBarcode")}
							<input
								type="text"
								autoFocus
								value={barcode}
								onChange={event => setBarcode(event.target.value)}
								onKeyDown={event => { if (event.key === "Enter") { event.preventDefault(); onScan(); } }}
							/>
						</label>
					</div>
				</div>
				<div className="panel receipt">
					<table aria-label={t("shoppingCart")}>
						<thead>
							<tr><th>{t("name")}</th><th className="number">{t("quantity")}</th><th className="number">{t("price")}</th><th /></tr>
						</thead>
						<tbody>
							{cart.map(line => (
								<tr key={line.item.code}>
									<td>{line.item.name}<small>{line.item.code}</small></td>
									<td className="number">{formatNumber(line.amount)}</td><td className="number">{formatNumber(line.item.price * line.amount)}</td>
									<td className="number"><button type="button" className="quiet" onClick={() => setCart(cart.filter(other => other !== line))}>{t("remove")}</button></td>
								</tr>
							))}
						</tbody>
						<tfoot>
							<tr className="total"><th>{t("total")}</th><td colSpan={2} className="number">{formatNumber(total)}</td><td /></tr>
						</tfoot>
					</table>
					<FailureMessage failure={failure} notFound="customerNotFound" />
					<button type="submit" className="primary" disabled={busy || cart.length === 0 || !customerId}>{t("sell")}</button>
				</div>
			</form>
		</Page>
	);
}
