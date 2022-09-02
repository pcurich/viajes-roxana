using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Core.Domain.Topics;
using Nop.Plugin.Payments.SafeTyPay.Data;
using Nop.Plugin.Payments.SafeTyPay.Domain;
using Nop.Plugin.Payments.SafeTyPay.Infrastructure;
using Nop.Plugin.Payments.SafeTyPay.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;
using Nop.Services.Topics;

namespace Nop.Plugin.Payments.SafeTyPay
{
    public class SafeTyPayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        public const int FAKE_RESULT_LENGTH = 101;
        public const string PAYMENT_METHOD = "Payments.SafeTyPay";

        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly SafeTyPayPaymentSettings _safeTyPayPaymentSettings;
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IStoreContext _storeContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOrderService _orderService;
        private readonly IScheduleTaskService _scheduleTaskService; 
        private readonly INotificationRequestService _notification;
        private readonly SafeTyPayHttpClient _safeTyPayHttpClient;
        private readonly ITopicService _topicService;

        #endregion Fields

        #region Ctor

        public SafeTyPayPaymentProcessor(
            ILocalizationService localizationService,
            SafeTyPayPaymentSettings safeTyPayPaymentSettings,
            ILogger logger,
            ISettingService settingService,
            IWebHelper webHelper,
            IStoreContext storeContext,
            IHttpContextAccessor httpContextAccessor,
            IOrderService orderService,
            IScheduleTaskService scheduleTaskService, INotificationRequestService notification,
            SafeTyPayHttpClient safeTyPayHttpClient,
            ITopicService topicService, IOrderTotalCalculationService orderTotalCalculationService
            )
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
            _storeContext = storeContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _orderService = orderService;
            _scheduleTaskService = scheduleTaskService; 
            _notification = notification;
            _safeTyPayHttpClient = safeTyPayHttpClient;
            _safeTyPayPaymentSettings = safeTyPayPaymentSettings;
            _topicService = topicService;
            _orderTotalCalculationService = orderTotalCalculationService;
        }

        #endregion Ctor

        #region Methods

        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            //always success
            return Task.FromResult(new CancelRecurringPaymentResult());
        }

        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            return Task.FromResult(new CapturePaymentResult { Errors = new[] { "Capture method not supported" } });
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
                _safeTyPayPaymentSettings.AdditionalFee, _safeTyPayPaymentSettings.AdditionalFeePercentage);
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            return Task.FromResult(new ProcessPaymentRequest());
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            if (_safeTyPayPaymentSettings.ApiKey.Length > 0 && _safeTyPayPaymentSettings.SignatureKey.Length > 0)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        /// <summary>
        /// Process a payment
        /// 1) Create a Request Express Token
        /// 2) Force Request to get the transactionCode
        /// 3) Delete a notification id exist (OrderGuid)
        /// 4) Insert a new Notification
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns> </returns>
        public async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest ppr) 
        {
            var result = new ProcessPaymentResult();
            try
            {
                #region Notification 
                var notification = new NotificationRequestTemp
                {
                    ApiKey = _safeTyPayPaymentSettings.ApiKey,
                    MerchantSalesID = ppr.OrderGuid.ToString()
                };
                await _notification.InsertNotificationRequest(notification);

                #endregion

                #region ExpressToken

                var requestExpressToken = await _safeTyPayHttpClient.GetExpressTokenRequest(ppr.CustomerId, ppr.OrderGuid.ToString(), ppr.OrderTotal);
                var strResult = WebUtility.UrlDecode(_safeTyPayHttpClient.GetExpressTokenResponse(requestExpressToken).Result);
                var responseExpressToken = SafeTyPayHelper.ToExpressTokenResponse(strResult);
                if (responseExpressToken == null)
                {
                    await _logger.ErrorAsync(string.Format(_localizationService.GetResourceAsync("Plugins.Payments.SafeTyPay.Error.ResponseExpressToken").Result, strResult));
                }

                #endregion ExpressToken

                #region request simulator

                var fakeResult = WebUtility.UrlDecode(_safeTyPayHttpClient.FakeHttpRequest(responseExpressToken.ClientRedirectURL).Result);

                #endregion request simulator

                #region Update Notification Table temporal

                notification = await _notification.GetNotificationRequestByMerchanId(ppr.OrderGuid);
                if (notification !=null)
                {
                    notification.ClientRedirectURL = responseExpressToken!=null ? responseExpressToken.ClientRedirectURL : "error";
                    notification.OperationCode = fakeResult.Length > FAKE_RESULT_LENGTH;
                };

                await _notification.UpdateNotificationRequest(notification);

                result = new ProcessPaymentResult
                {
                    AuthorizationTransactionId = responseExpressToken.ClientRedirectURL,
                    AuthorizationTransactionResult = "[OPERATION-CODE]"
                };

                #endregion Update Notification Table temporal
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync(string.Format(_localizationService.GetResourceAsync("Plugins.Payments.SafeTyPay.Error.ProcessPayment").Result, ex.StackTrace.ToString()));
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// PostProcessPayment
        /// 1) Request the Operation Code with the especific OrderGuid
        /// </summary>
        /// <param name="postProcessPaymentRequest"></param>
        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest pppr)
        {
            try
            {
                var order = pppr.Order;
                order.PaymentMethodSystemName = PAYMENT_METHOD;

                #region Check OperationCode

                var tmp = _notification.GetNotificationRequestByMerchanId(order.OrderGuid).Result;
                if (tmp == null)
                {
                    tmp =_notification.GetNotificationRequestByMerchanId(order.OrderGuid).Result; 
                }

                if (!tmp.OperationCode)
                {
                    var fakeResult = WebUtility.UrlDecode(_safeTyPayHttpClient.FakeHttpRequest(order.AuthorizationTransactionId).Result);
                    tmp.OperationCode = fakeResult.Length > FAKE_RESULT_LENGTH;
                }

                #endregion Check OperationCode

                #region Notification Token

                var requestNotificationToken = _safeTyPayHttpClient.GetNotificationRequest(order.OrderGuid.ToString());
                var strResult = WebUtility.UrlDecode(_safeTyPayHttpClient.GetNotificationResponse(requestNotificationToken).Result);
                var responseNotificationToken = SafeTyPayHelper.ToOperationActivityResponse(strResult, order.OrderGuid.ToString());
                if (responseNotificationToken == null)
                {
                    await _logger.ErrorAsync(string.Format(_localizationService.GetResourceAsync("Plugins.Payments.SafeTyPay.Error.ResponseOperationToken").Result, strResult));
                }

                #endregion Notification Token
                 
                order.AuthorizationTransactionResult = responseNotificationToken;
                order.AuthorizationTransactionCode = responseNotificationToken;
                tmp.PaymentReferenceNo = responseNotificationToken;

                _httpContextAccessor.HttpContext.Response.Redirect(tmp.ClientRedirectURL);
                _orderService.AddNote(_localizationService.GetResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequest").Result, responseNotificationToken);

                await _orderService.UpdateOrderAsync(order);
                await  _notification.UpdateNotificationRequest(tmp);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync(string.Format(_localizationService.GetResourceAsync("Plugins.Payments.SafeTyPay.Error.PostProcessPayment").Result, ex.StackTrace.ToString()));
                _httpContextAccessor.HttpContext.Response.Redirect(_storeContext.GetCurrentStore().Url);
            }

            return;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult { Errors = new[] { _localizationService.GetResourceAsync("Plugins.Payments.SafeTyPay.Error.RecurringPayment").Result } });
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            return Task.FromResult(new RefundPaymentResult { Errors = new[] { "Refund method not supported" } });
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentSafeTyPay/Configure";
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return "PaymentSafeTyPay";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override async Task InstallAsync()
        { 

            //settings
            await _settingService.SaveSettingAsync(new SafeTyPayPaymentSettings
            {
                UseSandbox = SafeTyPayDefaults.UseSandbox,
                ExpirationTime = SafeTyPayDefaults.ExpirationTime,

                AdditionalFee = SafeTyPayDefaults.AdditionalFee,
                AdditionalFeePercentage = SafeTyPayDefaults.AdditionalFeePercentage,

                TransactionOkURL = SafeTyPayDefaults.TransactionOkURL,
                TransactionErrorURL = SafeTyPayDefaults.TransactionErrorURL,

                UserNameMMS = "",
                PasswordMMS = "",

                PasswordTD = "",

                ApiKey = "",
                SignatureKey = "",
                ExpressTokenUrl = SafeTyPayDefaults.ExpressTokenUrl,
                NotificationUrl = SafeTyPayDefaults.NotificationUrl,
            });

            //ScheduleTask
            var task = new ScheduleTask
            {
                Name = SafeTyPayDefaults.SynchronizationTaskName,
                //60 minutes
                Seconds = SafeTyPayDefaults.SynchronizationPeriod,
                Type = SafeTyPayDefaults.SynchronizationTaskType,
                Enabled = SafeTyPayDefaults.SynchronizationEnabled,
                StopOnError = SafeTyPayDefaults.SynchronizationStopOnError
            };
            await _scheduleTaskService.InsertTaskAsync(task);

            //topic
            var topic = new Topic
            {
                Title = "SafeTyPayError",
                SystemName = "SafeTyPayError",
                DisplayOrder = 1,
                Body = "<p>We had some problems when processing your payment. Please try again or contact <a href='mailto:support@safetypay.com'>support@safetypay.com</a>  to help you.</p>"
            };

            await _topicService.InsertTopicAsync(topic);

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Configure", "Configure", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Configure", "Configuración", "es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.PaymentPending", "Payment Pending", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.PaymentPending", "Pago Pendiente", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.General", "General", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.General", "General", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UseSandbox", "Use Sandbox","en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UseSandbox", "Use Sandbox","es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UseSandbox.Hint", "Check to enable development mode.", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UseSandbox.Hint", "Marque para habilitar el modo de desarrollo.", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.CanGenerateNewCode", "Can Generate New Code", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.CanGenerateNewCode", "Puede generar un nuevo código", "es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.CanGenerateNewCode.Hint", "When safetypay announces that a code has expired, the system re-requests a new code from safetypay internally and announces the change to the customer by email and updates the purchase order with the new request.", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.CanGenerateNewCode.Hint", "cuando safetypay anuncia que un código ha caducado, el sistema vuelve a solicitar un nuevo código a safetypay de forma interna y anuncia al cliente de dicho cambio mediante email y actualiza la orden de compra con la nueva petición", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ExpirationTime", "Duration", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ExpirationTime", "Duración", "es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ExpirationTime.Hint", "Time in minutes to expire the operation code by safetypay.Value given in minutes: 90, 60, 1440, 30, etc", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ExpirationTime.Hint", "Tiempo en minutos para expirar el código de operación provisto por safetypay. Valor dado en minutos: 90, 60, 1440, 30, etc.", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFee", "Additional fee (Value)", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFee", "Cuota Adicional (Valor)", "es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFee.Hint", "Ingrese una tarifa adicional para cobrar a sus clientes.", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFeePercentage", "Additional fee (%)", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFeePercentage", "Cuota Adicional (%)", "es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFeePercentage.Hint", "Enter percentage additional fee to charge your customers.", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFeePercentage.Hint", "Ingrese un procentaje adicional para cobrar a sus clientes.", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Merchant.Management.System", "Merchant Management System", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Merchant.Management.System", "Sistema de Gestión Comercial", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UserNameMMS", "UserName", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UserNameMMS", "Usuario", "es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UserNameMMS.Hint", "Save UserName access", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UserNameMMS.Hint", "Guardar nombre de usuario", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordMMS", "Password", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordMMS", "Contraseña", "es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordMMS.Hint", "Save secret Password", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordMMS.Hint", "Guardar contraseña secreta", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Technical.Documentation", "Technical Documentation", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Technical.Documentation", "Documentación Técnica", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordTD", "Password", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordTD", "Contraseña", "es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordTD.Hint", "Save secret Password", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordTD.Hint", "Guardar contraseña secreta", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Environment", "Environment", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Environment", "Entorno", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ApiKey", "Api Key", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ApiKey", "Api Key", "es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ApiKey.Hint", "the ApiKey", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ApiKey.Hint", "La llave de la Api", "es-ES");
            
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.SignatureKey", "Signature Key", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.SignatureKey", "Signature Key", "es-ES");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.SignatureKey.Hint", "The Signature Key", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.SignatureKey.Hint", "The Signature Key", "es-ES");



            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.Case102", "Payment completed successfully with reference Code {0}", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.Case102", "Pago completado con éxito con el código de referencia {0}", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequest", "Send to SafetyPay for the code Operation {0}", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequest", "Se envio a SafetyPay por el codigo de operación {0}", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew1", "The code {0} was expired by SafetyPay", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew1", "El código {0}  fue caducado por SafetyPay", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew2", "The order with the operation code has expired {0}", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew2", "La orden con el código de operación {0} ha vencido", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew3", "System  request new code to SafeTyPay", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew3", "El sistema solicita un nuevo código a SafeTyPay", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew4", "SafetyPay send new operation code {0} to System", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew4", "SafetyPay send new operation code {0} to System", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.RedirectionTip", "You will be redirected to the SafetyPay site to obtain the operation number", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.RedirectionTip", "Será redirigido al sitio de SafetyPay para obtener el numero de operación", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.PaymentMethodDescription", "Code generated by SafeTyPay", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.PaymentMethodDescription", "Código geneado por SafeTyPay", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.ResponseExpressToken", "Error in Response Express Token {0}", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.ResponseExpressToken", "Error la respuesta del Express Token {0}", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.ResponseOperationToken", "Error in Response Opeation Token {0}", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.ResponseOperationToken", "Error en la respuesta de notificacion Token {0}", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.ProcessPayment", "Error in the Process Payment Executed {0}", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.ProcessPayment", "Error en la ejecución Process Payment {0}", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.PostProcessPayment", "Error in the Post Process Payment Executed {0}", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.PostProcessPayment", "Error en la ejecución Post Process Payment {0}", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.SchedulerTask.Execute", "Error in the executed task {0}", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.SchedulerTask.Execute", "Error en la ejecución de la tarea {0}", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.RecurringPayment", "Recurring payment not supported", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.RecurringPayment", "Pago recurrentes no soportados ", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.Refund", "Refund method not supported", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.Refund", "Método de reembolso no admitido", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.Void", "Void method not supported", "en-US");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.Void", "Método vacío no admitido", "es-ES");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Instructions", @"
            <p> If you are using this gateway, please make sure that SafeTyPay supports the currency of your main store. </p>
            <p> The <strong> general </strong> fields have the purpose of storing the credentials granted by SafeTyPay that give access to the <strong> Commercial Management System </strong> by means of a username and password which are sent by email. once the commercial management with the company has started. </p>
            <p> The password of the <strong> technical documentation </strong> provides access to the documentary portal that this company provides. </p>
            <p> In the <strong> Commercial Management System </strong> provided by SafeTyPay, go to the profile option located in the upper right, then in the left side menu select the credentials option there will generate the <strong> APIKEY </strong> and the <strong> SIGNATUREKEY </strong> that the system requires. </p>
            <p> Only in the case of development, In the Commercial Management System provided by SafeTyPay, go to the profile option located in the upper right, then in the left side menu select the option of notifications, enter an email so that you receive the notifications that SafeTyPay sends and in the <strong> PostUrl </strong> put the following value (" + _webHelper.GetStoreLocation() + @" Plugins/PaymentSafeTyPay/SafeTyPayAutomaticNotification). Also check the boxes for <strong> POST / WS and Email. </strong> </p>
            <p> The payments that SafeTyPay reports are <strong> asynchronous </strong>, this means that payment notifications are stored in the system and subsequently synchronized by means of a scheduled task that synchronizes payments from time to time, configurable in the next path to <a href='" + _webHelper.GetStoreLocation() + @"Admin/ScheduleTask/List'> scheduled tasks </a> </p>
            <p> To find out if you have notifications sent by SafeTyPay that have not yet been processed, <strong> see the section below </strong> </p>", "en-US");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.SafeTyPay.Instructions", @"
            <p>Si está utilizando esta puerta de enlace, asegúrese de que SafeTyPay admita la moneda de su tienda principal.</p>
            <p>Los campos <strong>generales</strong> tienen la finalidad de almacenar las credenciales otorgadas por SafeTyPay que dan acceso al <strong>Sistema de Gestión Comercial</strong> mediante un usuario y contraseña los cuales son enviado por correo una vez iniciada la gestión comercial con la empresa.</p>
            <p>La contraseña de la <strong>documentación técnica</strong> brinda el acceso al portal documentario que esta empresa brinda.</p>
            <p>En el <strong>Sistema de Gestión Comercial</strong> provisto por SafeTyPay, diríjase a la opción de perfil ubicada en la parte superior derecha, luego en el menú lateral de la izquierda seleccione la opción de credenciales ahí va a generar el <strong>APIKEY</strong> y el <strong>SIGNATUREKEY</strong> que el sistema requiere.</p>
            <p>Solo en el caso de desarrollo, En el Sistema de Gestión Comercial provisto por SafeTyPay, diríjase a la opción de perfil ubicada en la parte superior derecha, luego en el menú lateral de la izquierda seleccione la opción de notificaciones, ingrese un correo electrónico para que reciba las notificaciones que SafeTyPay envía y en el <strong>PostUrl</strong> coloque el siguiente valor ("+_webHelper.GetStoreLocation()+ @"Plugins/PaymentSafeTyPay/SafeTyPayAutomaticNotification). También active las casillas de <strong>POST/WS y Email.</strong></p>
            <p>Los pagos que SafeTyPay notifica son de forma <strong>asíncrona</strong>, esto quiere decir que las notificaciones de pagos son almacenadas en el sistema y posteriormente sincronizadas mediante una tarea programada que sincroniza los pagos cada cierto tiempo configurable en la siguiente ruta hacia las tareas <a href='"+_webHelper.GetStoreLocation()+ @"Admin/ScheduleTask/List'>programadas</a> </p>
            <p>Para saber si tiene notificaciones enviadas por SafeTyPay y que aún no han sido procesadas, <strong>vea el sección de abajo</strong></p>", "es-ES");
            
            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override async Task UninstallAsync()
        { 

            //settings
            await _settingService.DeleteSettingAsync<SafeTyPayPaymentSettings>();

            //ScheduleTask
            var task = await  _scheduleTaskService.GetTaskByTypeAsync(SafeTyPayDefaults.SynchronizationTaskType);
            if(task!=null)
                await _scheduleTaskService.DeleteTaskAsync(task);

            //Topic 
            var topic = await _topicService.GetTopicBySystemNameAsync("SafeTyPayError", showHidden:true);
            if (topic  != null)
                await _topicService.DeleteTopicAsync(topic);

            //locales
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Configure");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.PaymentPending");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.General");

            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UseSandbox");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UseSandbox.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ExpirationTime");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ExpirationTime.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFee");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFee.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFeePercentage");

            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.AdditionalFeePercentage.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Merchant.Management.System");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UserNameMMS");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.UserNameMMS.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordMMS");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordMMS.Hint");

            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Technical.Documentation");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordTD");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.PasswordTD.Hint");

            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Environment");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ApiKey");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.ApiKey.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.SignatureKey");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.SignatureKey.Hint");

            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.Case102");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequest");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew1");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew2");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew3");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Note.SendRequestExpiredNew4");

            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Fields.RedirectionTip");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.PaymentMethodDescription");

            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.ResponseExpressToken");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.ResponseOperationToken");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.ProcessPayment");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.PostProcessPayment");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.SchedulerTask.Execute");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.RecurringPayment");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.Refund");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Error.Void");

            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.SafeTyPay.Instructions");

            await base.UninstallAsync();
        }

        #endregion Methods 

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => false;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => false;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => false;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => false;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription => _localizationService.GetResourceAsync("Plugins.Payments.SafeTyPay.PaymentMethodDescription").Result;

        #endregion Properties

        public async Task ClearAsync(Guid guid)
        {
            var  requestByMerchanId = await  _notification.GetNotificationRequestByMerchanId(guid);
            if (requestByMerchanId == null)
                return;
            await _notification.DeleteNotificationRequest(requestByMerchanId);
        }
        
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.SafeTyPay.PaymentMethodDescription");
        }  

    }
}