import { send } from "./client";

export type LicenseStatus = "Trial" | "Active" | "GracePeriod" | "Locked";
export type LicensePlan = "None" | "Monthly" | "Yearly";

export type LicenseInfo = {
	status: LicenseStatus; plan: LicensePlan; trialEndsAtUtc: string; graceEndsAtUtc: string | null;
	subscriptionExpiresAtUtc: string | null; daysRemaining: number; monthlyPricePyg: number; yearlyPricePyg: number;
	machineId: string; discountPercent: number | null; effectivePricePyg: number | null;
};

export const licenseApi = {
	getLicense: (signal?: AbortSignal) => send<LicenseInfo>("/license", { signal }),
	activateLicense: (licenseKey: string) => send<LicenseInfo>("/license/activate", { method: "POST", body: JSON.stringify({ licenseKey }) })
};
