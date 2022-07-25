using System;
using Nop.Core;

namespace Nop.Plugin.Payments.PagoEfectivo.Domain
{
    public class NotificationPagoEfectivo : BaseEntity
    {
        public int OrderId { get; set; }
        public string Type { get; set; }
        public string OperationNumber { get; set; }
        public string Cip { get; set; }
        public string CipUrl { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public string PaymentDate { get; set; }
        public string Token { get; set; }
        public string TokenExpires { get; set; }
        public string TokenStart { get; set; }
        public string TransactionCode { get; set; }
        public string DocumentType { get; set; }
        public string DocumentTypeValue { get; set; }
        public string PhoneCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Description{ get; set; }
        public DateTime CreatedAt{ get; set; }
        public string Signature { get;  set; }
    }
}

