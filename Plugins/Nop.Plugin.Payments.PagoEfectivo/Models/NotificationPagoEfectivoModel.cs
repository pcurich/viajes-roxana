using System;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.PagoEfectivo.Models
{
    public record  NotificationPagoEfectivoModel: BaseNopEntityModel
    {
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.OrderId")]
        public int OrderId { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.OperationNumber")]
        public string OperationNumber { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.Cip")]
        public string Cip { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.CipUrl")]
        public string CipUrl { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.Currency")]
        public string Currency { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.Amount")]
        public decimal Amount { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.PaymentDate")]
        public string PaymentDate { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.TransactionCode")]
        public string TransactionCode { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.DocumentType")]
        public string DocumentType { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.DocumentTypeValue")]
        public string DocumentTypeValue { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.PhoneNumber")]
        public string PhoneNumber { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.PhoneCode")] 
        public string PhoneCode { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.Description")]
        public string Description { get; set; }
        [NopResourceDisplayName("Plugins.Payments.PagoEfectivo.Fields.CreatedAt")]
        public DateTime CreatedAt { get; set; }
    }
}
