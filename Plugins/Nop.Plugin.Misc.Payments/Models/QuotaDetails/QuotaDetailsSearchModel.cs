using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Payments.Models.QuotaDetails
{
    public record QuotaDetailsSearchModel : BaseSearchModel
    {
        public int QuotaId { get; set; }
    }
}
