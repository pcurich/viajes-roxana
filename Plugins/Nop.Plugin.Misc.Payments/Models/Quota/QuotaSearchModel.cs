using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Payments.Models.Quota
{
    public record QuotaSearchModel : BaseSearchModel
    {
        public QuotaSearchModel()
        {
            AvailableOrderStatus = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.SearchModel.OrderId")]
        public int OrderId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.SearchModel.OrderStatusId")]
        public int OrderStatusId { get; set; }

        public IList<SelectListItem> AvailableOrderStatus { get; set; }
    }
}
