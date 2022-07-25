using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Payments.Models.Quota
{
    public record QuotaModel : BaseNopEntityModel
    {
        public QuotaModel()
        {
            AvailableFrequencies = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.OrderId")]
        public int OrderId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.CustomerId")]
        public int CustomerId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.OrderStatusId")]
        public int OrderStatusId { get; set; }
        public string OrderStatus { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.Quotas")]
        public int Quotas { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.OrderTotal")]
        public string OrderTotal { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.StartTimeOn")]
        public DateTime StartTimeOn { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.FrecuencyId")]
        public int FrecuencyId { get; set; }
        public string Frecuency { get; set; }
        public IList<SelectListItem> AvailableFrequencies { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.UpdatedOn")]
        public DateTime UpdatedOn { get; set; }
    }
}
