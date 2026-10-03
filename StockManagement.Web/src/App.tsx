import { LogOut, Menu, Search } from "lucide-react";
import { useEffect, useState } from "react";
import type { Invoice } from "./api";
import { useAuth } from "./auth";
import { LoginPage } from "./features/auth/LoginPage";
import { customersRoute } from "./features/customers/route";
import { invoicesRoute } from "./features/invoices/route";
import { salesRoute } from "./features/sales/route";
import { reportsRoute } from "./features/reports/route";
import { CommandPalette } from "./features/search/CommandPalette";
import { companySettingsRoute, settingsRoute } from "./features/settings/routes";
import { stockItemsRoute } from "./features/stock-items/route";
import { suppliersRoute } from "./features/suppliers/route";
import { usersRoute } from "./features/users/route";
import { useI18n } from "./i18n";
import type { NavRoute, View } from "./routes";

// Same order and icons as the WPF menu (FontAwesome Wrench, AddressBook, Inbox)
const routes: NavRoute[] = [stockItemsRoute, customersRoute, suppliersRoute, salesRoute, invoicesRoute, reportsRoute, companySettingsRoute, settingsRoute, usersRoute];

export function App()
{
	const { t } = useI18n();
	const { username, logout, hasPermission } = useAuth();
	const [view, setView] = useState<View>("stockItems");
	const [invoice, setInvoice] = useState<Invoice>();
	const [menuExtended, setMenuExtended] = useState(true);
	const [paletteOpen, setPaletteOpen] = useState(false);

	function onSold(sold: Invoice)
	{
		setInvoice(sold);
		setView("invoices");
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

	if (!username) return <LoginPage />;

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
			<main>{activeRoute.render({ invoice, onSold })}</main>
		</div>
	);
}
