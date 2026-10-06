import { Component, createContext, useContext, useEffect, useState, type ContextType, type ReactNode } from "react";
import { api, setServerErrorHandler, type ReportCategory } from "./api";
import { Dialog } from "./Dialog";
import { useI18n } from "./i18n";
import { useToast } from "./Toast";

// Manual "report a problem" and the "send report?" prompt on an unhandled error share this one path to
// POST /feedback (ADR-0042): both end up calling submit() below with the same shape.
type DialogState = { open: boolean; category: ReportCategory; message: string; logExcerpt?: string; correlationId: string; locked: boolean };

const closed: DialogState = { open: false, category: "Feedback", message: "", correlationId: "", locked: false };

const FeedbackContext = createContext<{ openFeedbackDialog: () => void; reportError: (error: unknown, correlationId?: string) => void } | null>(null);

export function useFeedback()
{
	const context = useContext(FeedbackContext);
	if (!context) throw new Error("useFeedback needs a FeedbackProvider.");
	return context;
}

export function FeedbackProvider({ children }: { children: ReactNode })
{
	const { t } = useI18n();
	const { show } = useToast();
	const [state, setState] = useState<DialogState>(closed);
	const [busy, setBusy] = useState(false);

	function openFeedbackDialog()
	{
		setState({ open: true, category: "Feedback", message: "", correlationId: crypto.randomUUID(), locked: false });
	}

	function reportError(error: unknown, correlationId?: string)
	{
		const message = error instanceof Error ? error.message : String(error);
		const logExcerpt = error instanceof Error ? error.stack ?? error.message : String(error);
		setState({ open: true, category: "Bug", message, logExcerpt, correlationId: correlationId ?? crypto.randomUUID(), locked: true });
	}

	useEffect(() =>
	{
		setServerErrorHandler(correlationId => reportError(new Error("Server error"), correlationId));

		function onError(event: ErrorEvent)
		{
			reportError(event.error ?? event.message);
		}
		function onRejection(event: PromiseRejectionEvent)
		{
			reportError(event.reason);
		}

		window.addEventListener("error", onError);
		window.addEventListener("unhandledrejection", onRejection);
		return () =>
		{
			setServerErrorHandler(null);
			window.removeEventListener("error", onError);
			window.removeEventListener("unhandledrejection", onRejection);
		};
	}, []);

	async function onSend()
	{
		setBusy(true);
		const result = await api.submitFeedback(state.category, state.message, state.logExcerpt, state.correlationId);
		setBusy(false);
		setState(closed);
		show(result.ok && result.value.succeeded ? t("feedbackSent") : t("feedbackSendFailed"), result.ok && result.value.succeeded ? "success" : "danger");
	}

	return (
		<FeedbackContext.Provider value={{ openFeedbackDialog, reportError }}>
			{children}
			<Dialog open={state.open} onClose={() => setState(closed)} title={state.locked ? t("sendErrorReport") : t("reportProblem")}>
				<div className="form-grid">
					{!state.locked && (
						<label>
							{t("feedbackCategory")}
							<select value={state.category} onChange={event => setState({ ...state, category: event.target.value as ReportCategory })}>
								<option value="Feedback">{t("feedbackCategoryFeedback")}</option>
								<option value="Bug">{t("feedbackCategoryBug")}</option>
							</select>
						</label>
					)}
					<label>
						{t("feedbackMessage")}
						<textarea rows={5} placeholder={t("feedbackMessagePlaceholder")} value={state.message} onChange={event => setState({ ...state, message: event.target.value })} />
					</label>
				</div>
				<div className="form-actions">
					<button type="button" disabled={busy || !state.message.trim()} onClick={onSend}>{t("send")}</button>
					<button type="button" disabled={busy} onClick={() => setState(closed)}>{state.locked ? t("dontSend") : t("cancel")}</button>
				</div>
			</Dialog>
		</FeedbackContext.Provider>
	);
}

/** Catches render errors and offers to send a report for them, via the same FeedbackContext as everything else. */
export class ErrorBoundary extends Component<{ children: ReactNode }, { hasError: boolean }>
{
	static contextType = FeedbackContext;
	declare context: ContextType<typeof FeedbackContext>;

	state = { hasError: false };

	static getDerivedStateFromError()
	{
		return { hasError: true };
	}

	componentDidCatch(error: Error)
	{
		this.context?.reportError(error);
	}

	render()
	{
		if (this.state.hasError) return <ErrorFallback onReset={() => this.setState({ hasError: false })} />;
		return this.props.children;
	}
}

function ErrorFallback({ onReset }: { onReset: () => void })
{
	const { t } = useI18n();
	return (
		<div className="form-grid">
			<p>{t("errorOccurred")}</p>
			<button type="button" onClick={onReset}>{t("back")}</button>
		</div>
	);
}
