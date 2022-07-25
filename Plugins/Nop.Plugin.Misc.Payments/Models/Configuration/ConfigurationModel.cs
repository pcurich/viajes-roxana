using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Payments.Models.Configuration
{
    public record ConfigurationModel : BaseNopModel
    {
        #region Ctor

        public ConfigurationModel()
        {
            AvailableOrderStatus = new List<SelectListItem>();
            AvailableFrequencies = new List<SelectListItem>();
        }

        #endregion

        #region Properties

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.OrderStatusId")]
        public int OrderStatusId { get; set; }
        public bool OrderStatusId_OverrideForStore { get; set; }

        public IList<SelectListItem> AvailableOrderStatus { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.FrecuencyId")]
        public int FrecuencyId { get; set; }
        public bool FrecuencyId_OverrideForStore { get; set; }

        public IList<SelectListItem> AvailableFrequencies { get; set; }


        #endregion
    }
}
