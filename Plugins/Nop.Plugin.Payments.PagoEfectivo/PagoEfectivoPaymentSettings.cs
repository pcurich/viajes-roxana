using System;
using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.PagoEfectivo
{
    /// <summary>
    /// Represents settings of Pago Efectivo payment plugin
    /// </summary>
    public class PagoEfectivoPaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the end point in developer enviroment
        /// </summary>
        public string ApiDev { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the end point to get the authorization of Cip
        /// </summary>
        public string UrlAuthorizationToCip { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the end point to build Cip
        /// </summary>
        public string UrlCip { get; set; }

        public string ApiPrd { get; set; }

        /// <summary>
        /// Llave de acceso otorgado por PagoEfectivo.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Identificador del Comercio proporcionado por PagoEfectivo.
        /// </summary>
        public int IdService { get; set; }

        /// <summary>
        /// Llave secreta de acceso otorgado por PagoEfectivo.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Gets or sets payment transaction mode
        /// </summary>
        public TransactMode TransactMode { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }

        public string TokenStart { get; set; }
        public string TokenExpires { get; set; }
        public string CodeService { get; set; }
        public string Token { get; set; }
    }
}
