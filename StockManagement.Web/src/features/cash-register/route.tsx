import { Wallet } from "lucide-react";
import type { NavRoute } from "../../routes";
import { CashRegisterPage } from "./CashRegisterPage";

export const cashRegisterRoute: NavRoute = {
	name: "cashRegister",
	icon: Wallet,
	permission: "CashRegister.Read",
	render: () => <CashRegisterPage />
};
