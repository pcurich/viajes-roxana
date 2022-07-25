using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.PagoEfectivo.Models
{
    /// <summary>
    /// Represents notification Pago Efectivo list model
    /// </summary>
    public partial record NotificationPagoEfectivoListModel : BasePagedListModel<NotificationPagoEfectivoModel>
    {
    }
}
