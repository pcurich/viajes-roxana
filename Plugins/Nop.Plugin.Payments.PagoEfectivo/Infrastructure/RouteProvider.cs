using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.PagoEfectivo.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.PagoEfectivo.AutomaticNotification", "Plugins/PaymentPagoEfectivo/AutomaticNotification",
                 new { controller = "PaymentPagoEfectivo", action = "AutomaticNotification" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => -1;
    }
}
