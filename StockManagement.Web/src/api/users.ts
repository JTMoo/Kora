import { send, sendAllPages } from "./client";

export type UserRole = "Standard" | "Admin";

export type Permission =
	| "Users.Manage"
	| "Customers.Read" | "Customers.Write"
	| "StockItems.Read" | "StockItems.Write"
	| "Sales.Read" | "Sales.Write"
	| "Settings.Read" | "Settings.Write"
	| "Suppliers.Read" | "Suppliers.Write"
	| "Reports.Read";

export type User = { id: string; username: string; fullName: string; email: string; phone: string; position: string; role: UserRole; permissions: Permission[] };

export type NewUser = Partial<Omit<User, "id" | "role" | "permissions">> & { username: string; password: string; role?: UserRole; permissions?: Permission[] };

export type EditUser = Omit<User, "permissions"> & { permissions: Permission[]; password?: string };

export const usersApi = {
	listUsers: (signal?: AbortSignal) => sendAllPages<User>("/users", signal),
	createUser: (user: NewUser) => send<User>("/users", { method: "POST", body: JSON.stringify(user) }),
	updateUser: (user: EditUser) => send<User>(`/users/${user.id}`, { method: "PUT", body: JSON.stringify(user) }),
	deleteUser: (user: User) => send<void>(`/users/${user.id}`, { method: "DELETE" })
};
