using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Misc.Payments.Models.OrderNote;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Payments.Models.QuotaDetails
{
    public record QuotaDetailsModel : BaseNopEntityModel
    {
        public QuotaDetailsModel()
        {
            AvailablePaymentStatus = new List<SelectListItem>();
        }

        public int QuotaId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.OrderId")]
        public int OrderId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.OrderNoteId")]
        public int OrderNoteId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.CustomerId")]
        public int CustomerId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.Sequence")]
        public int Sequence { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.Amount")]
        public decimal Amount { get; set; }
        public string AmountStr { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.PaymentStatusId")]
        public int PaymentStatusId { get; set; }
        public string PaymentStatus { get; set; }
        public IList<SelectListItem> AvailablePaymentStatus { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.SchedulerDateOn")]
        public DateTime SchedulerDateOn { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.PaymentDateOn")]
        public DateTime? PaymentDateOn { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CustomQuota.Fields.UpdatedOn")]
        public DateTime UpdatedOn { get; set; }

        public OrderNoteModel OrderNoteModel { get; set; }

    }
}
