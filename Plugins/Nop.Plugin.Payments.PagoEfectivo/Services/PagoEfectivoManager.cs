using System;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;

namespace Nop.Plugin.Payments.PagoEfectivo.Services
{
    public class PagoEfectivoManager : IDisposable
    {
        #region Fields

        private readonly PagoEfectivoPaymentSettings _pagoEfectivoPaymentSettings;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly INotificationPagoEfectivoService _notification;
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;
        private readonly IEmailSender _emailSender;
        private readonly IStoreContext _storeContext;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IWorkContext _workContext;
        private readonly IOrderProcessingService _orderProcessingService;

        #endregion

        #region Ctor

        public PagoEfectivoManager(IEmailSender emailSender, PagoEfectivoPaymentSettings pagoEfectivoPaymentSettings, 
            EmailAccountSettings emailAccountSettings, INotificationPagoEfectivoService notification, IOrderService orderService,
            ILogger logger, IStoreContext storeContext, IEmailAccountService emailAccountService,
            IWorkContext workContext, IOrderProcessingService orderProcessingService)
        {
            _notification = notification;
            _orderService = orderService;
            _logger = logger;
            _storeContext = storeContext;
            _emailAccountService = emailAccountService;
            _workContext = workContext;
            _orderProcessingService = orderProcessingService;
            _emailSender = emailSender;
            _pagoEfectivoPaymentSettings = pagoEfectivoPaymentSettings;
            _emailAccountSettings = emailAccountSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sync Payment Async
        /// </summary>
        /// <returns></returns>
        public async Task SyncPaymentAsync()
        {
            await HandleFunctionAsync(async () =>
            {
                var notifications = await _notification.GetAllNotificationRequestAsync();
                foreach (var pagoSeguro in notifications)
                {
                    if (pagoSeguro.Signature!=null && pagoSeguro.Signature.Length > 0 && pagoSeguro.OrderId>0)
                    {
                        var order = await _orderService.GetOrderByIdAsync(pagoSeguro.OrderId);
                        if (order.AuthorizationTransactionId == pagoSeguro.Cip)
                        {
                            try
                            {
                                order.CaptureTransactionResult = pagoSeguro.OperationNumber;
                                order.CaptureTransactionId = pagoSeguro.TransactionCode;
                                order.OrderStatus = OrderStatus.Processing;

                                await _orderProcessingService.MarkOrderAsPaidAsync(order);
                                await _notification.DeleteNotificationRequestAsync(pagoSeguro);

                                var emailAccount = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);
                                var store = await _storeContext.GetCurrentStoreAsync();
                                var subject = store.Name + ". Pago realizado con codigo " + pagoSeguro.Cip;
                                var body = "Se ha realizado un pago: Numero de Nro de Orden: " + order.Id + " <a href='" + store.Url + "/Admin/Order/Edit/" + order.Id + "'> " + "Click aqui" + " </a>";
                                await _emailSender.SendEmailAsync(emailAccount, subject, body, emailAccount.Email, emailAccount.DisplayName, emailAccount.Email, null);
                            }catch(Exception ex)
                            {
                                var d = ex.Message;
                            }
                        }
                    }
                }
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
            return !string.IsNullOrEmpty(_pagoEfectivoPaymentSettings.SecretKey);
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
                await _logger.ErrorAsync($"{"Synchronization (PagoEfectivo plugin)"} error. {errorMessage}", exception, await _workContext.GetCurrentCustomerAsync());
                return default;
            }
        }

        #endregion

    }
}
