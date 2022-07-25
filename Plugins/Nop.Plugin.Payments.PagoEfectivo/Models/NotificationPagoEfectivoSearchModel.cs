using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.PagoEfectivo.Models
{
    public record  NotificationPagoEfectivoSearchModel : BaseSearchModel
    {
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.SearchModel.Order")]
        public int OrderId { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.SearchModel.Cip")]
        public string Cip { get; set; }
    }
}
