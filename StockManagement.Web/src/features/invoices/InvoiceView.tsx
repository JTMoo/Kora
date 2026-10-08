import { useEffect, useState, type FormEvent } from "react";
import { api, authHeaders, type ApiFailure, type Invoice, type Payment, type PaymentLink } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { invoiceStatusBadge } from "./invoiceStatus";
import { paymentLinkStatusBadge } from "./paymentLinkStatus";
import { RecordPaymentForm } from "./RecordPaymentForm";
import { Page } from "../../Page";
import { isTextKey, useI18n } from "../../i18n";

export function InvoiceView({ invoice: initial, onBack }: { invoice?: Invoice; onBack?: () => void })
{
	const { t, formatNumber, formatDate } = useI18n();
	const [number, setNumber] = useState(initial?.number ?? "");
	const [invoice, setInvoice] = useState(initial);
	const [failure, setFailure] = useState<ApiFailure>();
	const [cancelling, setCancelling] = useState(false);
	const [cancelReason, setCancelReason] = useState("");
	const [paymentLink, setPaymentLink] = useState<PaymentLink>();
	const [paymentLinkFailure, setPaymentLinkFailure] = useState<ApiFailure>();
	const [creatingPaymentLink, setCreatingPaymentLink] = useState(false);
	const [payments, setPayments] = useState<Payment[]>([]);
	const [recordingPayment, setRecordingPayment] = useState(false);
	const [kudeFailure, setKudeFailure] = useState<ApiFailure>();
	const [creditNoteNumber, setCreditNoteNumber] = useState<number>();

	useEffect(() => setInvoice(initial), [initial]);
	useEffect(() => setCancelling(false), [invoice?.number]);
	useEffect(() => setRecordingPayment(false), [invoice?.number]);

	useEffect(() =>
	{
		setPayments([]);
		if (!invoice) return;

		const controller = new AbortController();
		api.listPayments(invoice.number, controller.signal).then(result =>
		{
			if (controller.signal.aborted) return;
			if (result.ok) setPayments(result.value.items);
		});
		return () => controller.abort();
	}, [invoice?.number]);

	useEffect(() =>
	{
		setPaymentLink(undefined);
		setPaymentLinkFailure(undefined);
		if (!invoice) return;

		const controller = new AbortController();
		api.getPaymentLink(invoice.number, controller.signal).then(result =>
		{
			if (controller.signal.aborted) return;
			if (result.ok) setPaymentLink(result.value);
		});
		return () => controller.abort();
	}, [invoice?.number]);

	async function onCreatePaymentLink()
	{
		setCreatingPaymentLink(true);
		const result = await api.createPaymentLink(invoice!.number);
		setCreatingPaymentLink(false);
		// PaymentLinkRejectedResponse.Reason is a resource key (not a pre-rendered message like other 409s)
		if (!result.ok) return setPaymentLinkFailure(result.failure.kind === "invalidState"
			? { kind: "invalidState", reason: t(isTextKey(result.failure.reason) ? result.failure.reason : "unexpectedError") }
			: result.failure);

		setPaymentLinkFailure(undefined);
		setPaymentLink(result.value);
	}

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		const result = await api.getInvoice(number);
		setInvoice(result.ok ? result.value : undefined);
		setFailure(result.ok ? undefined : result.failure);
	}

	async function onPaymentRecorded()
	{
		setRecordingPayment(false);
		const result = await api.getInvoice(invoice!.number);
		if (result.ok) setInvoice(result.value);

		const paymentsResult = await api.listPayments(invoice!.number);
		if (paymentsResult.ok) setPayments(paymentsResult.value.items);
	}

	async function onViewKude()
	{
		setKudeFailure(undefined);
		const response = await fetch(`/api/invoices/${encodeURIComponent(invoice!.number)}/kude`, { headers: authHeaders() });
		if (!response.ok)
		{
			setKudeFailure(response.status === 404 ? { kind: "notFound" } : { kind: "invalidState", reason: t("kudeUnavailable") });
			return;
		}

		const url = URL.createObjectURL(await response.blob());
		window.open(url, "_blank");
	}

	async function onCancelInvoice(event: FormEvent)
	{
		event.preventDefault();
		const result = await api.cancelInvoice(invoice!.number, cancelReason);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		setCancelling(false);
		setCancelReason("");
		setCreditNoteNumber(result.value.number);
		setInvoice({ ...invoice!, isCancelled: true });
	}

	return (
		<Page title={t("invoices")} toolbar={
			<form onSubmit={onSubmit} className="toolbar-form">
				{onBack && <button type="button" className="quiet" onClick={onBack}>{t("back")}</button>}
				<input type="text" required aria-label={t("invoiceId")} placeholder={t("invoiceId")} value={number} onChange={event => setNumber(event.target.value)} />
				<button type="submit">{t("show")}</button>
			</form>
		}>
			<FailureMessage failure={failure} notFound="invoiceNotFound" />
			{invoice && (
				<article className="paper" aria-label={`${t("invoice")} ${invoice.number}`}>
					<header>
						<h3>{t("invoice")} {invoice.number}</h3>
						<span>{t(invoice.saleCondition === "Cash" ? "cash" : "credit")}</span>
						{invoiceStatusBadge(invoice, t)}
					</header>
					<dl>
						<dt>{t("customerName")}</dt><dd>{invoice.customerName} ({invoice.customerId})</dd>
						<dt>{t("creationDate")}</dt><dd>{formatDate(invoice.date)}</dd>
						<dt>{t("expirationDate")}</dt><dd>{formatDate(invoice.expirationDate)}</dd>
					</dl>
					<table>
						<thead>
							<tr><th>{t("code")}</th><th>{t("name")}</th><th className="number">{t("quantity")}</th><th className="number">{t("price")}</th></tr>
						</thead>
						<tbody>
							{invoice.lines.map(line => (
								<tr key={line.code}>
									<td>{line.code}</td><td>{line.name}</td><td className="number">{formatNumber(line.amount)}</td><td className="number">{formatNumber(line.unitPrice)}</td>
								</tr>
							))}
						</tbody>
						<tfoot>
							<tr><th colSpan={3}>{t("tax")}</th><td className="number">{formatNumber(invoice.tax)}</td></tr>
							<tr className="total"><th colSpan={3}>{t("total")}</th><td className="number" data-testid="invoice-total">{formatNumber(invoice.total)}</td></tr>
						</tfoot>
					</table>
					{!invoice.isCancelled && !cancelling && (
						<div className="form-actions">
							<button type="button" onClick={() => setCancelling(true)}>{t("cancelInvoice")}</button>
							<button type="button" onClick={onViewKude}>{t("viewKude")}</button>
						</div>
					)}
					<FailureMessage failure={kudeFailure} />
					{invoice.isCancelled && creditNoteNumber !== undefined && (
						<p>{t("creditNote")}: {creditNoteNumber}</p>
					)}
					{!invoice.isCancelled && cancelling && (
						<form onSubmit={onCancelInvoice} className="form-grid">
							<label>
								{t("reason")}
								<input type="text" required value={cancelReason} onChange={event => setCancelReason(event.target.value)} />
							</label>
							<div className="form-actions">
								<button type="submit">{t("cancelInvoice")}</button>
								<button type="button" onClick={() => setCancelling(false)}>{t("cancel")}</button>
							</div>
						</form>
					)}
					{!invoice.isCancelled && (
						<section aria-label={t("paymentLink")}>
							<h4>{t("paymentLink")}</h4>
							<FailureMessage failure={paymentLinkFailure} />
							{paymentLink ? (
								<dl>
									<dt>{t("paymentLinkAmount")}</dt><dd>{formatNumber(paymentLink.amount)}</dd>
									<dt>{t("status")}</dt><dd>{paymentLinkStatusBadge(paymentLink, t)}</dd>
									<dt>{t("paymentLinkCreated")}</dt><dd>{formatDate(paymentLink.createdAt)}</dd>
									<dt>{t("paymentLinkExpires")}</dt><dd>{formatDate(paymentLink.expiresAt)}</dd>
									<dd><a href={paymentLink.qrUrl} target="_blank" rel="noreferrer">{t("paymentLinkOpen")}</a></dd>
								</dl>
							) : (
								<div className="form-actions">
									<button type="button" disabled={creatingPaymentLink} onClick={onCreatePaymentLink}>{t("generatePaymentLink")}</button>
								</div>
							)}
						</section>
					)}
					<section aria-label={t("payments")}>
						<h4>{t("payments")}</h4>
						{payments.length > 0 && (
							<table>
								<thead>
									<tr><th>{t("paymentDate")}</th><th>{t("paymentMethod")}</th><th className="number">{t("paymentAmount")}</th></tr>
								</thead>
								<tbody>
									{payments.map((payment, index) => (
										<tr key={index}>
											<td>{formatDate(payment.date)}</td><td>{t(payment.method === "BankTransfer" ? "bankTransfer" : payment.method === "BancardQr" ? "bancardQr" : payment.method === "Check" ? "check" : payment.method === "Other" ? "other" : "cash")}</td><td className="number">{formatNumber(payment.amount)}</td>
										</tr>
									))}
								</tbody>
							</table>
						)}
						{!invoice.isCancelled && invoice.amountDue > 0 && !recordingPayment && (
							<div className="form-actions">
								<button type="button" onClick={() => setRecordingPayment(true)}>{t("recordPayment")}</button>
							</div>
						)}
						{recordingPayment && (
							<RecordPaymentForm invoiceNumber={invoice.number} onPaid={onPaymentRecorded} onCancel={() => setRecordingPayment(false)} />
						)}
					</section>
				</article>
			)}
		</Page>
	);
}
