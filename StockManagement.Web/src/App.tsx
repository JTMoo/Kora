import { LogOut, Menu, Search } from "lucide-react";
import { useEffect, useState } from "react";
import { api, setLicenseLockedHandler, type Invoice, type LicenseInfo } from "./api";
import { useAuth } from "./auth";
import { LoginPage } from "./features/auth/LoginPage";
import { cashRegisterRoute } from "./features/cash-register/route";
import { customersRoute } from "./features/customers/route";
import { goodsImportsRoute } from "./features/goods-imports/route";
import { invoicesRoute } from "./features/invoices/route";
import { LicenseBanner } from "./features/licensing/LicenseBanner";
import { LicenseLockedScreen } from "./features/licensing/LicenseLockedScreen";
import { licensingRoute } from "./features/licensing/route";
import { marangatuExportRoute } from "./features/marangatu/route";
import { payablesRoute } from "./features/payables/route";
import { remissionNotesRoute } from "./features/remission-notes/route";
import { salesRoute } from "./features/sales/route";
import { reportsRoute } from "./features/reports/route";
import { CommandPalette } from "./features/search/CommandPalette";
import { companySettingsRoute, settingsRoute } from "./features/settings/routes";
import { stockItemsRoute } from "./features/stock-items/route";
import { suppliersRoute } from "./features/suppliers/route";
import { usersRoute } from "./features/users/route";
import { useI18n } from "./i18n";
import type { NavRoute, View } from "./routes";
import { useLoad } from "./useLoad";

// Same order and icons as the WPF menu (FontAwesome Wrench, AddressBook, Inbox)
const routes: NavRoute[] = [stockItemsRoute, customersRoute, suppliersRoute, salesRoute, invoicesRoute, remissionNotesRoute, goodsImportsRoute, marangatuExportRoute, payablesRoute, cashRegisterRoute, reportsRoute, companySettingsRoute, settingsRoute, usersRoute, licensingRoute];

export function App()
{
	const { username } = useAuth();

	// A separate component, mounted only once logged in: its hooks (the license fetch below) must not run,
	// and so must not fetch with a missing auth token, while the login screen is still showing.
	if (!username) return <LoginPage />;
	return <AuthenticatedApp />;
}

function AuthenticatedApp()
{
	const { t } = useI18n();
	const { logout, hasPermission } = useAuth();
	const [view, setView] = useState<View>("stockItems");
	const [invoice, setInvoice] = useState<Invoice>();
	const [menuExtended, setMenuExtended] = useState(true);
	const [paletteOpen, setPaletteOpen] = useState(false);
	const { data: license, setData: setLicense } = useLoad(api.getLicense);
	const [forceLocked, setForceLocked] = useState(false);

	function onSold(sold: Invoice)
	{
		setInvoice(sold);
		setView("invoices");
	}

	function onLicenseActivated(activated: LicenseInfo)
	{
		setLicense(activated);
		setForceLocked(false);
	}

	const visibleRoutes = routes.filter(route => !route.permission || hasPermission(route.permission));

	useEffect(() =>
	{
		function onKeyDown(event: KeyboardEvent)
		{
			if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k")
			{
				event.preventDefault();
				setPaletteOpen(true);
			}
		}
		window.addEventListener("keydown", onKeyDown);
		return () => window.removeEventListener("keydown", onKeyDown);
	}, []);

	// Any API call can trip the 402 lockout mid-session, not just the GET /license poll below
	useEffect(() =>
	{
		setLicenseLockedHandler(() => setForceLocked(true));
		return () => setLicenseLockedHandler(null);
	}, []);

	if (forceLocked || license?.status === "Locked") return <LicenseLockedScreen onActivated={onLicenseActivated} />;

	const activeRoute = visibleRoutes.find(route => route.name === view) ?? visibleRoutes[0];

	return (
		<div className="shell">
			{paletteOpen && (
				<CommandPalette routes={visibleRoutes} onNavigate={setView} onClose={() => setPaletteOpen(false)} />
			)}
			<nav className={menuExtended ? "sidebar" : "sidebar collapsed"}>
				<button className="sidebar-item" aria-label="Menu" aria-expanded={menuExtended} onClick={() => setMenuExtended(!menuExtended)}>
					<Menu />
				</button>
				<button className="sidebar-item" aria-label={t("search")} onClick={() => setPaletteOpen(true)}>
					<Search />{menuExtended && <span>{t("searchBoxDefault")}</span>}
				</button>
				<div className="sidebar-items">
					{visibleRoutes.map(({ name, icon: Icon }) => (
						<button key={name} className="sidebar-item" aria-label={t(name)} aria-pressed={view === name} onClick={() => setView(name)}>
							<Icon />{menuExtended && <span>{t(name)}</span>}
						</button>
					))}
				</div>
				<div className="sidebar-bottom">
					<button className="sidebar-item" aria-label={t("logout")} onClick={logout}>
						<LogOut />{menuExtended && <span>{t("logout")}</span>}
					</button>
				</div>
			</nav>
			<div className="content">
				{license && <LicenseBanner license={license} onManage={() => setView("licensing")} />}
				<main>{activeRoute.render({ invoice, onSold })}</main>
			</div>
		</div>
	);
}
