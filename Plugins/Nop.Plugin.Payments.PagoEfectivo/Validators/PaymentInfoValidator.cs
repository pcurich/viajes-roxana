using System;
using FluentValidation;
using Nop.Plugin.Payments.PagoEfectivo.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.PagoEfectivo.Validators
{
    public partial class PaymentInfoValidator : BaseNopValidator<PaymentInfoModel>
    {
        public PaymentInfoValidator(ILocalizationService localizationService)
        {
            //useful links:
            //http://fluentvalidation.codeplex.com/wikipage?title=Custom&referringTitle=Documentation&ANCHOR#CustomValidator
            //http://benjii.me/2010/11/credit-card-validator-attribute-for-asp-net-mvc-3/

            RuleFor(x => x.DocumentType).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.Fields.DocumentType.Required"));
            RuleFor(x => x.DocumentTypeValue).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.Fields.DocumentType.Required"));
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.Fields.PhoneNumber.Required"));
            RuleFor(x => x.PhoneCode).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.Fields.PhoneCode.Required"));
        }
    }
}
