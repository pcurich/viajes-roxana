using System;
using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Izipay
{
    /// <summary>
    /// Represents settings of Izipay payment plugin
    /// </summary>
    public class IzipayPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }
        public string UrlCreateToken { get; set; }
        public string User { get; set; }
        public string DevPassword { get; set; }
        public string ProdPassword { get; set; }



    }
}
