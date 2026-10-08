import { send } from "./client";
import type { Permission, UserRole } from "./users";

export type LoginResult = { token: string; username: string; role: UserRole; permissions: Permission[]; mustChangePassword: boolean };

export const authApi = {
	login: (username: string, password: string) => send<LoginResult>("/auth/login", { method: "POST", body: JSON.stringify({ username, password }) }),
	changePassword: (currentPassword: string, newPassword: string) => send<void>("/auth/password", { method: "PUT", body: JSON.stringify({ currentPassword, newPassword }) })
};
