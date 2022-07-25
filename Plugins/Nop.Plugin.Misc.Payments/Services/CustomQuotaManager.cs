using System;
using System.Threading.Tasks;
using Nop.Core;

namespace Nop.Plugin.Misc.Payments.Services
{
    public class CustomQuotaManager : IDisposable
    {
        #region Fields 

        #endregion

        #region Ctor

        public CustomQuotaManager()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sync Payment Async
        /// </summary>
        /// <returns></returns>
        public async Task SyncCustomQuotaAsync()
        {
            await HandleFunctionAsync(async () =>
            {
                //var notifications = await _notification.GetAllNotificationRequestAsync();
                //foreach (var pagoSeguro in notifications)
                //{
                //    if (pagoSeguro.Signature != null && pagoSeguro.Signature.Length > 0 && pagoSeguro.OrderId > 0)
                //    {
                //        var order = await _orderService.GetOrderByIdAsync(pagoSeguro.OrderId);
                //        if (order.AuthorizationTransactionId == pagoSeguro.Cip)
                //        {

                //        }
                //    }
                //}
                return true;
            });
        }

        /// <summary>
        /// Dispose object
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Common

        /// <summary>
        /// Check that tax provider is configured
        /// </summary>
        /// <returns>True if it's configured; otherwise false</returns>
        private bool IsConfigured()
        {
            return true;
            //return !strKing.IsNullOrEmpty(_pagoEfectivoPaymentSettings.SecretKey);
        }

        /// <summary>
        /// Handle function and get result
        /// </summary>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <param name="function">Function</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        private async Task<TResult> HandleFunctionAsync<TResult>(Func<Task<TResult>> function)
        {
            try
            {
                //ensure that Avalara tax provider is configured
                if (!IsConfigured())
                    throw new NopException("Tax provider is not configured");

                return await function();
            }
            catch (Exception exception)
            {
                //compose an error message
                var errorMessage = exception.Message;
                //log errors
                //await _logger.ErrorAsync($"{"Synchronization (CustomQuota plugin)"} error. {errorMessage}", exception, await _workContext.GetCurrentCustomerAsync());
                return default;
            }
        }

        #endregion
    }
}
