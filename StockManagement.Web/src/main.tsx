import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { AuthProvider } from "./auth";
import { ErrorBoundary, FeedbackProvider } from "./Feedback";
import { I18nProvider } from "./i18n";
import { ToastProvider } from "./Toast";
import "./index.css";

createRoot(document.getElementById("root")!).render(
	<StrictMode>
		<I18nProvider>
			<AuthProvider>
				<ToastProvider>
					<FeedbackProvider>
						<ErrorBoundary>
							<App />
						</ErrorBoundary>
					</FeedbackProvider>
				</ToastProvider>
			</AuthProvider>
		</I18nProvider>
	</StrictMode>
);
