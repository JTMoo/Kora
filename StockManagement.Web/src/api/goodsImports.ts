import { send } from "./client";

export type Incoterm = "Exw" | "Fca" | "Fob" | "Cfr" | "Cif" | "Cpt" | "Cip" | "Dap" | "Ddp";

export const incoterms: Incoterm[] = ["Exw", "Fca", "Fob", "Cfr", "Cif", "Cpt", "Cip", "Dap", "Ddp"];

export type GoodsImportDocumentLine = { code: string; name: string; amount: number };

export type GoodsImportDocument = { id: string; proformaNumber: string; incoterm: Incoterm; brokerName: string; duaReference: string; date: string; supplierId: string; supplierName: string; items: GoodsImportDocumentLine[] };

export type NewGoodsImportDocument = { proformaNumber: string; supplierId: string; incoterm: Incoterm; brokerName: string; duaReference: string; date?: string; items: { code: string; amount: number }[] };

export type GoodsImportDocumentListResult = { items: GoodsImportDocument[]; nextCursor: string | null };

export type GoodsImportDocumentFilter = { cursor?: string; pageSize: number };

export const goodsImportApi = {
	listGoodsImportDocuments: (filter: GoodsImportDocumentFilter, signal?: AbortSignal) => send<GoodsImportDocumentListResult>(`/goods-import-documents?${goodsImportFilterQuery(filter)}`, { signal }),
	getGoodsImportDocument: (id: string, signal?: AbortSignal) => send<GoodsImportDocument>(`/goods-import-documents/${encodeURIComponent(id)}`, { signal }),
	createGoodsImportDocument: (document: NewGoodsImportDocument) => send<GoodsImportDocument>("/goods-import-documents", { method: "POST", body: JSON.stringify(document) })
};

function goodsImportFilterQuery(filter: GoodsImportDocumentFilter): string
{
	const params = new URLSearchParams({ pageSize: String(filter.pageSize) });
	if (filter.cursor) params.set("cursor", filter.cursor);
	return params.toString();
}
