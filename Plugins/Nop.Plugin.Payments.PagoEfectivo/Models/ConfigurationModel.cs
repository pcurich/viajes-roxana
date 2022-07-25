using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.PagoEfectivo.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.ApiDev")]
        public string ApiDev { get; set; }
        public bool ApiDev_OverrideForStore { get; set; }
        
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.ApiPrd")]
        public string ApiPrd { get; set; }
        public bool ApiPrd_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.UrlAuthorizationToCip")]
        public string UrlAuthorizationToCip { get; set; }
        public bool UrlAuthorizationToCip_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.UrlCip")]
        public string UrlCip { get; set; }
        public bool UrlCip_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.AccessKey")]
        public string AccessKey { get; set; }
        public bool AccessKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.IdService")]
        public int IdService { get; set; }
        public bool IdService_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.SecretKey")]
        public string SecretKey { get; set; }
        public bool SecretKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

    }
}
