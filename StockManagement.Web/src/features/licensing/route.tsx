import { KeyRound } from "lucide-react";
import type { NavRoute } from "../../routes";
import { LicensingPage } from "./LicensingPage";

export const licensingRoute: NavRoute = {
	name: "licensing",
	icon: KeyRound,
	render: () => <LicensingPage />
};
