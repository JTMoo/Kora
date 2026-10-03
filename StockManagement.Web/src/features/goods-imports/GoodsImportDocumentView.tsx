import type { GoodsImportDocument } from "../../api";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";

export function GoodsImportDocumentView({ document, onBack }: { document: GoodsImportDocument; onBack: () => void })
{
	const { t, formatDate } = useI18n();

	return (
		<Page title={t("goodsImports")} toolbar={
			<button type="button" className="quiet" onClick={onBack}>{t("back")}</button>
		}>
			<article className="paper" aria-label={`${t("goodsImportDocument")} ${document.proformaNumber}`}>
				<header>
					<h3>{t("goodsImportDocument")} {document.proformaNumber}</h3>
				</header>
				<dl>
					<dt>{t("supplier")}</dt><dd>{document.supplierName}</dd>
					<dt>{t("documentDate")}</dt><dd>{formatDate(document.date)}</dd>
					<dt>{t("incoterm")}</dt><dd>{document.incoterm.toUpperCase()}</dd>
					<dt>{t("brokerName")}</dt><dd>{document.brokerName}</dd>
					<dt>{t("duaReference")}</dt><dd>{document.duaReference}</dd>
				</dl>
				<table>
					<thead>
						<tr><th>{t("code")}</th><th>{t("name")}</th><th className="number">{t("quantity")}</th></tr>
					</thead>
					<tbody>
						{document.items.map(line => (
							<tr key={line.code}>
								<td>{line.code}</td><td>{line.name}</td><td className="number">{line.amount}</td>
							</tr>
						))}
					</tbody>
				</table>
			</article>
		</Page>
	);
}
