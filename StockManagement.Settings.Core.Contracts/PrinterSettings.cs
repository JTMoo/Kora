using StockManagement.Kernel.Model.Types;

namespace StockManagement.Settings.Core.Contracts;


/// <summary>
/// Printer settings used when printing a receipt (#220)
/// </summary>
/// <param name="DefaultPrinterName">Informational label for the printer to use; the browser's print dialog picks the actual printer</param>
/// <param name="ReceiptPaperWidthMm">Paper width the receipt layout is formatted for, in mm (e.g. 80 or 58)</param>
/// <param name="KudeFormat">Layout the KuDE is printed in</param>
/// <param name="PrintOnSaleComplete">Whether the receipt print dialog opens automatically once a sale completes</param>
public sealed record PrinterSettings(string DefaultPrinterName, int ReceiptPaperWidthMm, KudeFormat KudeFormat, bool PrintOnSaleComplete);
