using System.ComponentModel.DataAnnotations;
using StockManagement.Language;

namespace StockManagement.Kernel.Model.Types;


/// <summary>
/// Layout the KuDE (printable DTE) is rendered in
/// </summary>
public enum KudeFormat
{
	[Display(ResourceType = typeof(Settings), Name = nameof(Settings.ticket))]
	Ticket,

	[Display(ResourceType = typeof(Settings), Name = nameof(Settings.a4))]
	A4
}
