using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Plugin.Payments.PagoEfectivo.Models;
using Nop.Services.Common;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.PagoEfectivo.Components
{
    public class PaymentPagoEfectivoViewComponent : NopViewComponent
    {
        #region Fields

        private readonly IAddressService _addressService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public PaymentPagoEfectivoViewComponent(IAddressService addressService, IWorkContext workContext)
        {
            _addressService = addressService;
            _workContext = workContext;
        }

        #endregion

        #region Methods
        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var customerPhoneNumber = string.Empty;
            if (customer.BillingAddressId.HasValue)
            {
                var address = await _addressService.GetAddressByIdAsync(customer.BillingAddressId.Value);
                customerPhoneNumber = address.PhoneNumber;
            };

            var customerDocument = customer.Username != string.Empty && customer.Username.Length > 0
                ? customer.Username
                : string.Empty;

            var model = new PaymentInfoModel()
            {
                DocumentTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "DNI", Text = "Documento nacional de identidad" },
                    new SelectListItem { Value = "NAN", Text = "Pasaporte" },
                    new SelectListItem { Value = "PAR", Text = "Partida" },
                    new SelectListItem { Value = "LMI", Text = "Libreta militar" },
                    new SelectListItem { Value = "NAN", Text = "otros" },
                },
                PhoneNumber = customerPhoneNumber,
                PhoneCode = "+51",
                DocumentTypeValue = customerDocument

            };

            return View("~/Plugins/Payments.PagoEfectivo/Views/PaymentInfo.cshtml", model);
        }
        #endregion
    }
}
