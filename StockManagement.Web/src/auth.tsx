import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { api, setAuthToken, setUnauthorizedHandler, type Permission, type Result, type LoginResult, type UserRole } from "./api";

const tokenKey = "auth.token";
const usernameKey = "auth.username";
const roleKey = "auth.role";
const permissionsKey = "auth.permissions";

type Auth = {
	username: string | null;
	role: UserRole | null;
	permissions: Permission[];
	hasPermission: (permission: Permission) => boolean;
	login: (username: string, password: string) => Promise<Result<LoginResult>>;
	logout: () => void;
};

const AuthContext = createContext<Auth | null>(null);

function readStorage(key: string): string | null
{
	try
	{
		return localStorage.getItem(key);
	}
	catch
	{
		return null;
	}
}

function writeStorage(key: string, value: string | null)
{
	try
	{
		if (value === null) localStorage.removeItem(key);
		else localStorage.setItem(key, value);
	}
	catch
	{
		// Private window / blocked storage: session just won't survive a refresh
	}
}

function readPermissions(): Permission[]
{
	try
	{
		return JSON.parse(readStorage(permissionsKey) ?? "[]") as Permission[];
	}
	catch
	{
		return [];
	}
}

export function AuthProvider({ children }: { children: ReactNode })
{
	// Set during render (not an effect): a child's own mount effect - e.g. a license status fetch - can otherwise run before the token is applied and 401
	const [username, setUsername] = useState<string | null>(() =>
	{
		const token = readStorage(tokenKey);
		if (token) setAuthToken(token);
		return readStorage(usernameKey);
	});
	const [role, setRole] = useState<UserRole | null>(() => readStorage(roleKey) as UserRole | null);
	const [permissions, setPermissions] = useState<Permission[]>(readPermissions);

	useEffect(() =>
	{
		setUnauthorizedHandler(logout);
		return () => setUnauthorizedHandler(null);
	}, []);

	async function login(usernameInput: string, password: string): Promise<Result<LoginResult>>
	{
		const result = await api.login(usernameInput, password);
		if (result.ok)
		{
			setAuthToken(result.value.token);
			setUsername(result.value.username);
			setRole(result.value.role);
			setPermissions(result.value.permissions);
			writeStorage(tokenKey, result.value.token);
			writeStorage(usernameKey, result.value.username);
			writeStorage(roleKey, result.value.role);
			writeStorage(permissionsKey, JSON.stringify(result.value.permissions));
		}
		return result;
	}

	function logout()
	{
		setAuthToken(null);
		setUsername(null);
		setRole(null);
		setPermissions([]);
		writeStorage(tokenKey, null);
		writeStorage(usernameKey, null);
		writeStorage(roleKey, null);
		writeStorage(permissionsKey, null);
	}

	function hasPermission(permission: Permission)
	{
		return permissions.includes(permission);
	}

	return <AuthContext.Provider value={{ username, role, permissions, hasPermission, login, logout }}>{children}</AuthContext.Provider>;
}

export function useAuth(): Auth
{
	const auth = useContext(AuthContext);
	if (!auth) throw new Error("useAuth needs an AuthProvider.");
	return auth;
}
