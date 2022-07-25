using System;
using Nop.Core;
using Nop.Core.Domain.Payments;

namespace Nop.Plugin.Misc.Payments.Domain.Quota
{
    public partial class QuotaDetails : BaseEntity
    {
        public int QuotaId { get; set; }
        public int OrderId { get; set; }
        public int OrderNoteId { get; set; }
        public int CustomerId { get; set; }
        public int Sequence { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }
        public int PaymentStatusId { get; set; }
        public PaymentStatus PaymentStatus
        {
            get => (PaymentStatus)PaymentStatusId;
            set => PaymentStatusId = (int)value;
        }
        public DateTime SchedulerDateOnUtc { get; set; }
        public DateTime? PaymentDateOnUtc { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

    }
}
