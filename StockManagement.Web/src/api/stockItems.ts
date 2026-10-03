import { send, sendAllPages } from "./client";

export type StockItem = { id: string; code: string; name: string; description: string; location: string; amount: number; price: number; manufacturer: string; factor: number; purchasePrice: number; purchaseExchangeRate: number; additionalPurchaseCost: number; supplierId?: string; supplierName?: string; minimumStock: number; barcode: string };

export type NewStockItem = Partial<Omit<StockItem, "id" | "code">> & { code: string; name: string };

export const stockItemsApi = {
	listStockItems: (signal?: AbortSignal) => sendAllPages<StockItem>("/stock-items", signal),
	getStockItemByBarcode: (barcode: string) => send<StockItem>(`/stock-items/by-barcode/${encodeURIComponent(barcode)}`),
	listStockItemsBelowMinimum: (signal?: AbortSignal) => sendAllPages<StockItem>("/stock-items/below-minimum", signal),
	createStockItem: (stockItem: NewStockItem) => send<StockItem>("/stock-items", { method: "POST", body: JSON.stringify(stockItem) }),
	updateStockItem: (stockItem: StockItem) => send<StockItem>(`/stock-items/${encodeURIComponent(stockItem.id)}`, { method: "PUT", body: JSON.stringify(stockItem) }),
	deleteStockItem: (stockItem: StockItem) => send<void>(`/stock-items/${encodeURIComponent(stockItem.id)}`, { method: "DELETE" }),
	checkInStockItem: (id: string, amount: number, reason: string) => send<StockItem>(`/stock-items/${encodeURIComponent(id)}/check-in`, { method: "POST", body: JSON.stringify({ amount, reason }) }),
	checkOutStockItem: (id: string, amount: number, reason: string) => send<StockItem>(`/stock-items/${encodeURIComponent(id)}/check-out`, { method: "POST", body: JSON.stringify({ amount, reason }) })
};
