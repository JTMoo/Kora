import { useState } from "react";
import type { GoodsImportDocument } from "../../api";
import { GoodsImportDocumentList } from "./GoodsImportDocumentList";
import { GoodsImportDocumentView } from "./GoodsImportDocumentView";

export function GoodsImportDocumentBrowser()
{
	const [selected, setSelected] = useState<GoodsImportDocument>();

	if (selected) return <GoodsImportDocumentView document={selected} onBack={() => setSelected(undefined)} />;
	return <GoodsImportDocumentList onSelect={setSelected} />;
}
