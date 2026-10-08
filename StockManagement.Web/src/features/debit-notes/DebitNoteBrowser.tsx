import { useState } from "react";
import type { DebitNote } from "../../api";
import { DebitNoteList } from "./DebitNoteList";
import { DebitNoteView } from "./DebitNoteView";

export function DebitNoteBrowser()
{
	const [selected, setSelected] = useState<DebitNote>();

	if (selected) return <DebitNoteView debitNote={selected} onBack={() => setSelected(undefined)} />;
	return <DebitNoteList onSelect={setSelected} />;
}
