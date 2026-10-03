import { useState } from "react";
import type { RemissionNote } from "../../api";
import { RemissionNoteList } from "./RemissionNoteList";
import { RemissionNoteView } from "./RemissionNoteView";

export function RemissionNoteBrowser()
{
	const [selected, setSelected] = useState<RemissionNote>();

	if (selected) return <RemissionNoteView remissionNote={selected} onBack={() => setSelected(undefined)} />;
	return <RemissionNoteList onSelect={setSelected} />;
}
