using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.Payments
{
    public class CustomQuotaSettings : ISettings
    {
        public int OrderStatusId { get; set; }
        public int FrecuencyId { get; set; }
    }
}
