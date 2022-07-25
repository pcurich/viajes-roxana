using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;


namespace Nop.Plugin.Payments.PagoEfectivo.Models
{
    public record PaymentInfoModel : BaseNopModel
    {
        public PaymentInfoModel()
        {
            DocumentTypes = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.DocumentType")]
        public string DocumentType { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.DocumentType")]
        public IList<SelectListItem> DocumentTypes { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.DocumentTypeValue")]
        public string DocumentTypeValue { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.PhoneNumber")]
        public string PhoneNumber { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.PhoneCode")]
        public string PhoneCode { get; set; }
        
    }
}
