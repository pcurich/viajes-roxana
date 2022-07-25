using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Payments.Models.OrderNote
{
    public partial record OrderNoteModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.Orders.OrderNotes.Fields.Note")]
        public string AddOrderNoteMessage { get; set; }

        [NopResourceDisplayName("Admin.Orders.OrderNotes.Fields.Download")]
        [UIHint("Download")]
        public int AddOrderNoteDownloadId { get; set; }

        public bool AddOrderNoteHasDownload { get; set; }

        [NopResourceDisplayName("Admin.Orders.OrderNotes.Fields.DisplayToCustomer")]
        public bool AddOrderNoteDisplayToCustomer { get; set; }

        public int OrderNoteId { get; set; }

    }
}
