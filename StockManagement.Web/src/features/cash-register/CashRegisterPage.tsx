import { useEffect, useState } from "react";
import { api, type ApiFailure, type CashRegisterCloseReport, type CashRegisterSession } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useAuth } from "../../auth";
import { useI18n } from "../../i18n";
import { useToast } from "../../Toast";
import { CashMovementForm } from "./CashMovementForm";
import { CashRegisterSessionHistory } from "./CashRegisterSessionHistory";
import { CloseSessionForm } from "./CloseSessionForm";
import { OpenSessionForm } from "./OpenSessionForm";

export function CashRegisterPage()
{
	const { t, formatNumber, formatDate } = useI18n();
	const { hasPermission } = useAuth();
	const { show: showToast } = useToast();
	const [session, setSession] = useState<CashRegisterSession>();
	const [failure, setFailure] = useState<ApiFailure>();
	const [closeReport, setCloseReport] = useState<CashRegisterCloseReport>();
	const [refreshToken, setRefreshToken] = useState(0);
	const canWrite = hasPermission("CashRegister.Write");

	useEffect(() =>
	{
		const controller = new AbortController();
		api.getCurrentCashRegisterSession(controller.signal).then(result =>
		{
			if (controller.signal.aborted) return;
			if (result.ok) return setSession(result.value);
			setSession(undefined);
			setFailure(result.failure.kind === "notFound" ? undefined : result.failure);
		});
		return () => controller.abort();
	}, [refreshToken]);

	function refresh()
	{
		setRefreshToken(token => token + 1);
	}

	function onOpened()
	{
		showToast(t("openSession"));
		refresh();
	}

	function onMovementAdded()
	{
		refresh();
	}

	function onClosed(report: CashRegisterCloseReport)
	{
		setCloseReport(report);
		setSession(undefined);
		refresh();
	}

	return (
		<Page title={t("cashRegister")}>
			<FailureMessage failure={failure} />

			{closeReport && (
				<div className="panel">
					<h3>{t("closeOutReport")}</h3>
					<dl className="form-grid">
						<div><dt>{t("expectedAmount")}</dt><dd>{formatNumber(closeReport.expectedAmount)}</dd></div>
						<div><dt>{t("countedAmount")}</dt><dd>{formatNumber(closeReport.countedAmount)}</dd></div>
						<div><dt>{t("difference")}</dt><dd>{formatNumber(closeReport.difference)}</dd></div>
					</dl>
					<div className="form-actions">
						<button type="button" onClick={() => setCloseReport(undefined)}>{t("close")}</button>
					</div>
				</div>
			)}

			{!session && canWrite && <OpenSessionForm onOpened={onOpened} />}

			{session && (
				<>
					<div className="panel">
						<dl className="form-grid">
							<div><dt>{t("openedAt")}</dt><dd>{formatDate(session.openedAt)}</dd></div>
							<div><dt>{t("openingFloat")}</dt><dd>{formatNumber(session.openingFloat)}</dd></div>
							<div><dt>{t("expectedAmount")}</dt><dd>{formatNumber(session.expectedAmount)}</dd></div>
						</dl>
					</div>

					<table>
						<thead>
							<tr><th>{t("movementType")}</th><th className="number">{t("amount")}</th><th>{t("reason")}</th><th>{t("openedAt")}</th></tr>
						</thead>
						<tbody>
							{session.movements.map((movement, index) => (
								<tr key={index}>
									<td>{t(movement.type === "In" ? "cashIn" : "cashOut")}</td>
									<td className="number">{formatNumber(movement.amount)}</td>
									<td>{movement.reason}</td>
									<td>{formatDate(movement.date)}</td>
								</tr>
							))}
						</tbody>
					</table>

					{canWrite && (
						<>
							<CashMovementForm sessionId={session.id} onAdded={onMovementAdded} />
							<CloseSessionForm sessionId={session.id} onClosed={onClosed} />
						</>
					)}
				</>
			)}

			<CashRegisterSessionHistory refreshToken={refreshToken} />
		</Page>
	);
}
