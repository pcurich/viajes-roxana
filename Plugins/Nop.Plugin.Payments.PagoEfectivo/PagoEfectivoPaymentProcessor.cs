using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.PagoEfectivo.Components;
using Nop.Plugin.Payments.PagoEfectivo.Domain;
using Nop.Plugin.Payments.PagoEfectivo.Models;
using Nop.Plugin.Payments.PagoEfectivo.Services;
using Nop.Plugin.Payments.PagoEfectivo.Validators;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Payments.PagoEfectivo
{
    public class PagoEfectivoPaymentProcessor : BasePlugin, IPaymentMethod, IAdminMenuPlugin
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;
        private readonly IPaymentService _paymentService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IWebHelper _webHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILanguageService _languageService;
        private readonly INotificationPagoEfectivoService _notification;
        private readonly PagoEfectivoHttpClient _pagoEfectivoHttpClient;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPermissionService _permissionService;
        private PagoEfectivoPaymentSettings _pagoEfectivoPaymentSettings;

        #endregion

        #region Ctor

        public PagoEfectivoPaymentProcessor(ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService, IOrderService orderService,
            ISettingService settingService, PagoEfectivoHttpClient pagoEfectivoHttpClient,
            INotificationPagoEfectivoService notification, IPaymentPluginManager paymentPluginManager,
            IWebHelper webHelper,ILanguageService languageService, IHttpContextAccessor httpContextAccessor,
            PagoEfectivoPaymentSettings pagoEfectivoPaymentSettings, IScheduleTaskService scheduleTaskService,
            IPermissionService permissionService, IPaymentService paymentService,ILogger logger)
        {
            _localizationService = localizationService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _orderService = orderService;
            _settingService = settingService;
            _webHelper = webHelper;
            _notification = notification;
            _paymentService = paymentService;
            _languageService = languageService;
            _pagoEfectivoPaymentSettings = pagoEfectivoPaymentSettings;
            _pagoEfectivoHttpClient = pagoEfectivoHttpClient;
            _httpContextAccessor = httpContextAccessor;
            _paymentPluginManager = paymentPluginManager;
            _scheduleTaskService = scheduleTaskService;
            _permissionService = permissionService;
            _logger = logger;
        }

        #endregion

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
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.Manual;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            _pagoEfectivoPaymentSettings = _pagoEfectivoHttpClient.AuthorizationsAsync().Result;

            var result = new ProcessPaymentResult
            {
                AuthorizationTransactionCode = _pagoEfectivoPaymentSettings.CodeService,
                AuthorizationTransactionResult = _pagoEfectivoPaymentSettings.Token
            };
            switch (_pagoEfectivoPaymentSettings.TransactMode)
            {
                case TransactMode.Pending:
                    result.NewPaymentStatus = PaymentStatus.Pending;
                    break;
                case TransactMode.Authorize:
                    result.NewPaymentStatus = PaymentStatus.Authorized;
                    break;
                case TransactMode.AuthorizeAndCapture:
                    result.NewPaymentStatus = PaymentStatus.Paid;
                    break;
                default:
                    result.AddError("Not supported transaction type");
                    break;
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            try
            {
                var orderItems = _orderService.GetOrderItemsAsync(postProcessPaymentRequest.Order.Id).Result;
                var order = postProcessPaymentRequest.Order;
                var productName = "";

                foreach (var orderItem in orderItems)
                {
                    var product = _orderService.GetProductByOrderItemIdAsync(orderItem.Id).Result;
                    productName = productName + "|" + product.Name;
                }

                productName = productName.Substring(1, productName.Length - 1);

                var (cipUrl, cip, transactionCode, code, message) = await _pagoEfectivoHttpClient.GetCipAsync(postProcessPaymentRequest.Order, productName);
                await _orderService.InsertOrderNoteAsync(new OrderNote { OrderId = order.Id, Note = cip.ToString(), DisplayToCustomer = true, CreatedOnUtc = DateTime.UtcNow });
                await _orderService.InsertOrderNoteAsync(new OrderNote { OrderId = order.Id, Note = cipUrl, DisplayToCustomer = true, CreatedOnUtc = DateTime.UtcNow });

                order.AuthorizationTransactionResult = cipUrl;
                order.AuthorizationTransactionId = cip;
                order.AuthorizationTransactionCode = transactionCode;
                order.CaptureTransactionResult = message;
                order.CaptureTransactionId = code;
                order.OrderStatus = OrderStatus.Pending;

                await _orderService.UpdateOrderAsync(order);
                var xml = _paymentService.DeserializeCustomValues(order);

                xml.TryGetValue(_localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.DocumentType").Result, out var documentType);
                xml.TryGetValue(_localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.DocumentTypeValue").Result, out var documentTypeValue);
                xml.TryGetValue(_localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.PhoneNumber").Result, out var phoneNumber);
                xml.TryGetValue(_localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.PhoneCode").Result, out var phoneCode);

                var notification = new NotificationPagoEfectivo
                {
                    OrderId = order.Id,
                    Cip = cip,
                    CipUrl = cipUrl,
                    OperationNumber = code,
                    TransactionCode = transactionCode,
                    Currency = order.CustomerCurrencyCode,
                    Amount = Math.Round(order.OrderTotal, 2),
                    DocumentType = Convert.ToString(documentType),
                    DocumentTypeValue = Convert.ToString(documentTypeValue),
                    PhoneCode = Convert.ToString(phoneCode),
                    PhoneNumber = Convert.ToString(phoneNumber),
                    Description = productName,
                    CreatedAt = DateTime.UtcNow,
                    Type = PagoEfectivoDefaults.DefaultAutomatic
                };
                await _notification.InsertNotificationRequestAsync(notification);

                if ("100" == code)
                {
                    _httpContextAccessor.HttpContext.Response.Redirect(cipUrl);
                }
                else
                {
                    _httpContextAccessor.HttpContext.Response.Redirect($"{_webHelper.GetStoreLocation()}order/history");
                }
            }
            catch(Exception ex)
            {
                await _logger.InsertLogAsync(LogLevel.Error, ex.Message, ex.StackTrace);
            }
            
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue - hide; false - display.
        /// </returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            if (_pagoEfectivoPaymentSettings.SecretKey.Length > 0 && _pagoEfectivoPaymentSettings.AccessKey.Length > 0)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the additional handling fee
        /// </returns>
        public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
                _pagoEfectivoPaymentSettings.AdditionalFee, _pagoEfectivoPaymentSettings.AdditionalFeePercentage);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the capture payment result
        /// </returns>
        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            return Task.FromResult(new CapturePaymentResult { Errors = new[] { "Capture method not supported" } });
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            return Task.FromResult(new RefundPaymentResult { Errors = new[] { "Refund method not supported" } });
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                AllowStoringCreditCardNumber = true
            };
            switch (_pagoEfectivoPaymentSettings.TransactMode)
            {
                case TransactMode.Pending:
                    result.NewPaymentStatus = PaymentStatus.Pending;
                    break;
                case TransactMode.Authorize:
                    result.NewPaymentStatus = PaymentStatus.Authorized;
                    break;
                case TransactMode.AuthorizeAndCapture:
                    result.NewPaymentStatus = PaymentStatus.Paid;
                    break;
                default:
                    result.AddError("Not supported transaction type");
                    break;
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            //always success
            return Task.FromResult(new CancelRecurringPaymentResult());
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return Task.FromResult(false);

            return Task.FromResult(true);

            //it's not a redirection payment method. So we always return false
            //return Task.FromResult(false);
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of validating errors
        /// </returns>
        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                DocumentType = form["DocumentType"],
                DocumentTypeValue= form["DocumentTypeValue"],
                PhoneNumber = form["PhoneNumber"],
                PhoneCode = form["PhoneCode"],
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return Task.FromResult<IList<string>>(warnings);
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the payment info holder
        /// </returns>
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            var customValues = new Dictionary<string, object>
            {
                { _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.DocumentType").Result, form["DocumentType"].ToString() },
                { _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.DocumentTypeValue").Result,  form["DocumentTypeValue"].ToString() },
                { _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.PhoneNumber").Result, form["PhoneNumber"].ToString() },
                { _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.PhoneCode").Result, form["PhoneCode"].ToString() }
            };

            return Task.FromResult(new ProcessPaymentRequest { CustomValues = customValues });
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentPagoEfectivo/Configure";
        }

        /// <summary>
        /// Gets a type of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component type</returns>
        public string GetPublicViewComponentName()
        {
            return "PaymentPagoEfectivo";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            //settings
            await _settingService.SaveSettingAsync(new PagoEfectivoPaymentSettings
            {
                UseSandbox = true,
                ApiDev = "https://pre1a.services.pagoefectivo.pe/",
                ApiPrd = "",
                UrlAuthorizationToCip= "v1/authorizations",
                UrlCip = "v1/cips",
                AccessKey = "ZmQ0YmNlY2Y4NzQ0NTQ5",
                IdService = 1615,
                SecretKey = "ye8Cm2RcFIYz6Z7gDTd8M51v6A46fGDXxKZ1GKHP"
            });

            //schedule task
            if (await _scheduleTaskService.GetTaskByTypeAsync(typeof(SynchronizationTask).FullName) is null)
            {
                await _scheduleTaskService.InsertTaskAsync(new()
                {
                    Enabled = true,
                    LastEnabledUtc = DateTime.UtcNow,
                    StopOnError=false,
                    Seconds = 60*60,
                    Name = PagoEfectivoDefaults.SystemNameSynchronization,
                    Type = typeof(SynchronizationTask).FullName
                });
            }

            //locales

            var en = new Dictionary<string, string>
            {
                ["Plugins.Payments.PagoEfectivo.Fields.UseSandbox"] = "Use Sandbox",
                ["Plugins.Payments.PagoEfectivo.Fields.UseSandbox.Hint"] = "Check to enable Sandbox (testing environment).",
                ["Plugins.Payments.PagoEfectivo.Fields.ApiDev"] = "End Point Dev",
                ["Plugins.Payments.PagoEfectivo.Fields.ApiDev.Hint"] = "End Point URL Develop",
                ["Plugins.Payments.PagoEfectivo.Fields.ApiPrd"] = "End Point Prod",
                ["Plugins.Payments.PagoEfectivo.Fields.ApiPrd.Hint"] = "End Point URL Producction",
                ["Plugins.Payments.PagoEfectivo.Fields.UrlAuthorizationToCip"] = "Path of authorization of Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.UrlAuthorizationToCip.Hint"] = "The end path to get the authorization of Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.UrlCip"] = "Path of build Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.UrlCip.Hint"] = "The end path to get the build Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.AccessKey"] = "Access Key (PagoEfectivo)",
                ["Plugins.Payments.PagoEfectivo.Fields.AccessKey.Hint"] = "Access Key provider by PagoEfectivo",
                ["Plugins.Payments.PagoEfectivo.Fields.IdService"] = "Id Service (PagoEfectivo)",
                ["Plugins.Payments.PagoEfectivo.Fields.IdService.Hint"] = "Id Service provider by PagoEfectivo",
                ["Plugins.Payments.PagoEfectivo.Fields.SecretKey"] = "Secret Key (PagoEfectivo)",
                ["Plugins.Payments.PagoEfectivo.Fields.SecretKey.Hint"] = "Secret Key provider by PagoEfectivo",
                ["Plugins.Payments.PagoEfectivo.Fields.AdditionalFee"] = "Additional fee",
                ["Plugins.Payments.PagoEfectivo.Fields.AdditionalFee.Hint"] = "Enter additional fee to charge your customers.",
                ["Plugins.Payments.PagoEfectivo.Fields.AdditionalFeePercentage"] = "Additional fee. Use percentage",
                ["Plugins.Payments.PagoEfectivo.Fields.AdditionalFeePercentage.Hint"] = "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.",
                ["Plugins.Payments.PagoEfectivo.PaymentMethodDescription"] = "Create a payment code",
                ["Plugins.Payments.PagoEfectivo.Instructions"] = @"This payment method create a number of payment to be payed at bank",
                ["Plugins.Payments.PagoEfectivo.RedirectionTip"] = "You will be redirected to the SafetyPay site to obtain the operation number",
                ["Plugins.Payments.PagoEfectivo.DocumentType"] = "Document Type",
                ["Plugins.Payments.PagoEfectivo.DocumentTypeValue"] = "Document Number",
                ["Plugins.Payments.PagoEfectivo.PhoneNumber"] = "Phone Number",
                ["Plugins.Payments.PagoEfectivo.PhoneCode"] = "Country Code",
                ["Plugins.Payments.PagoEfectivo.Configuration.Credentials"] = "Credentials",
                ["Plugins.Payments.PagoEfectivo.Configuration.CustomPayment"] = "Custom Payments",
                ["Plugins.Payments.PagoEfectivo.Menu.Title"] = "Custom Payments",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentType.Required"] = "Document Type required",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentTypeValue.Required"] = "Document Type is required",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneNumber.Required"] = "Phone number is required",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneCode.Required"] = "Phone code is required",
                ["Plugins.Payments.PagoEfectivo.SearchModel.Cip"] = "Cip",
                ["Plugins.Payments.PagoEfectivo.SearchModel.Cip.Hint"] = "Cip Code",
                ["Plugins.Payments.PagoEfectivo.SearchModel.Order"] = "OrderId",
                ["Plugins.Payments.PagoEfectivo.SearchModel.Order.Hint"] = "Identifier of Order ID",
                ["Plugins.Payments.PagoEfectivo.Fields.OrderId"] = "ID Order",
                ["Plugins.Payments.PagoEfectivo.Fields.OrderId.Hint"] = "ID of  registrada",
                ["Plugins.Payments.PagoEfectivo.Fields.Cip"] = "Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.Cip.Hint"] = "Code Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.CipUrl"] = "Link",
                ["Plugins.Payments.PagoEfectivo.Fields.CipUrl.Hint"] = "Link",
                ["Plugins.Payments.PagoEfectivo.Fields.Currency"] = "Currency",
                ["Plugins.Payments.PagoEfectivo.Fields.Currency.Hint"] = "Currency Denomination (PEN,USD)",
                ["Plugins.Payments.PagoEfectivo.Fields.Amount"] = "Amount",
                ["Plugins.Payments.PagoEfectivo.Fields.Amount.Hint"] = "Amount to collect",
                ["Plugins.Payments.PagoEfectivo.Fields.OperationNumber"] = "Operation Number",
                ["Plugins.Payments.PagoEfectivo.Fields.OperationNumber.Hint"] = "Cash Payment Transaction Number",
                ["Plugins.Payments.PagoEfectivo.Fields.TransactionCode"] = "Transaction Code",
                ["Plugins.Payments.PagoEfectivo.Fields.TransactionCode.Hint"] = "Transaction Code",
                ["Plugins.Payments.PagoEfectivo.Fields.PaymentDate"] = "Payment Date",
                ["Plugins.Payments.PagoEfectivo.Fields.PaymentDate.Hint"] = "Payment Date",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneNumber"] = "Phone Number",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneNumber.Hint"] = "Phone Number to send sms",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneCode"] = "Phone Code",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneCode.Hint"] = "Phone Code (51 in peru)",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentType"] = "Document Type",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentType.Hint"] = "Document Type (DNI)",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentTypeValue"] = "Document Type Value",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentTypeValue.Hint"] = "Document Type Value",
                ["Plugins.Payments.PagoEfectivo.Fields.Description"]= "Description",
                ["Plugins.Payments.PagoEfectivo.Fields.Description.Hint"] = "Description of Payment",
                ["Plugins.Payments.PagoEfectivo.Fields.CreatedAt"] = "CreatedAt",
                ["Plugins.Payments.PagoEfectivo.Fields.CreatedAt.Hint"] = "Created at",
                ["Plugins.Payments.PagoEfectivo.CipList"] = "List Cips",
                ["Plugins.Payments.PagoEfectivo.AddNewCip"] = "New Cip",
                ["Plugins.Payments.PagoEfectivo.Edit"] = "Edit CIP",
                ["Plugins.Payments.PagoEfectivo.Create"] = "Creat CIP",
                ["Plugins.Payments.PagoEfectivo.BackToList"] = "back to list",
                ["Plugins.Payments.PagoEfectivo.Deleted"] = "Notification deleted"
            };

            var es = new Dictionary<string, string>
            {
                ["Plugins.Payments.PagoEfectivo.Fields.UseSandbox"] = "Usar Sandbox",
                ["Plugins.Payments.PagoEfectivo.Fields.UseSandbox.Hint"] = "Activa el Sandbox (ambiente de desarrollo).",
                ["Plugins.Payments.PagoEfectivo.Fields.ApiDev"] = "End Point Dev",
                ["Plugins.Payments.PagoEfectivo.Fields.ApiDev.Hint"] = "End Point URL Desarrollo",
                ["Plugins.Payments.PagoEfectivo.Fields.ApiPrd"] = "End Point Prod",
                ["Plugins.Payments.PagoEfectivo.Fields.ApiPrd.Hint"] = "End Point URL Producción",
                ["Plugins.Payments.PagoEfectivo.Fields.UrlAuthorizationToCip"] = "Path de autorización de Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.UrlAuthorizationToCip.Hint"] = "El path para obtener una autorización de Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.UrlCip"] = "Path para  Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.UrlCip.Hint"] = "El path para construir el Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.AccessKey"] = "Llave de acceso (PagoEfectivo)",
                ["Plugins.Payments.PagoEfectivo.Fields.AccessKey.Hint"] = "Llave de acceso provista por PagoEfectivo",
                ["Plugins.Payments.PagoEfectivo.Fields.IdService"] = "Id del servicio (PagoEfectivo)",
                ["Plugins.Payments.PagoEfectivo.Fields.IdService.Hint"] = "Id del servicio provista por PagoEfectivo",
                ["Plugins.Payments.PagoEfectivo.Fields.SecretKey"] = "Llave secreta (PagoEfectivo)",
                ["Plugins.Payments.PagoEfectivo.Fields.SecretKey.Hint"] = "Llave secreta provista por PagoEfectivo",
                ["Plugins.Payments.PagoEfectivo.Fields.AdditionalFee"] = "Cargo adicional",
                ["Plugins.Payments.PagoEfectivo.Fields.AdditionalFee.Hint"] = "Ingrese un cargo adicional a cargar a un cliente.",
                ["Plugins.Payments.PagoEfectivo.Fields.AdditionalFeePercentage"] = "Cargo adicional. Porcentaje",
                ["Plugins.Payments.PagoEfectivo.Fields.AdditionalFeePercentage.Hint"] = "Determina si se aplica una tarifa adicional porcentual al total del pedido. Si no está habilitado, se utiliza un valor fijo.",
                ["Plugins.Payments.PagoEfectivo.PaymentMethodDescription"] = "Crea un código de pago",
                ["Plugins.Payments.PagoEfectivo.Instructions"] = @"Este método de pago crea un numero de pago para ser pagado en un banco",
                ["Plugins.Payments.PagoEfectivo.RedirectionTip"] = "Será redirigido al sitio de SafetyPay para obtener el numero de operación",
                ["Plugins.Payments.PagoEfectivo.DocumentType"] = "Tipo de Documento",
                ["Plugins.Payments.PagoEfectivo.DocumentTypeValue"] = "Número de documento",
                ["Plugins.Payments.PagoEfectivo.PhoneNumber"] = "Número de celular",
                ["Plugins.Payments.PagoEfectivo.PhoneCode"] = "Código de Pais",
                ["Plugins.Payments.PagoEfectivo.Configuration.Credentials"] = "Credenciales",
                ["Plugins.Payments.PagoEfectivo.Configuration.CustomPayment"] = "Pagos Personalizados",
                ["Plugins.Payments.PagoEfectivo.Menu.Title"] = "Pagos Personalizados",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentType.Required"] = "Tipo de documento es requerido",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentTypeValue.Required"] = "El numero de documento es requerido",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneNumber.Required"] = "Número de telefono es requerido",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneCode.Required"] = "Código de Pais es requerido",
                ["Plugins.Payments.PagoEfectivo.SearchModel.Cip"] = "Cip",
                ["Plugins.Payments.PagoEfectivo.SearchModel.Cip.Hint"] = "Código Cip",
                ["Plugins.Payments.PagoEfectivo.SearchModel.Order"] = "# de Orden",
                ["Plugins.Payments.PagoEfectivo.SearchModel.Order.Hint"] = "Identificador de la Orden de compra",
                ["Plugins.Payments.PagoEfectivo.Fields.OrderId"] = "ID Compra",
                ["Plugins.Payments.PagoEfectivo.Fields.OrderId.Hint"] = "ID de la Compra registrada",
                ["Plugins.Payments.PagoEfectivo.Fields.Cip"] = "Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.Cip.Hint"] = "Código Cip",
                ["Plugins.Payments.PagoEfectivo.Fields.CipUrl"] = "Link",
                ["Plugins.Payments.PagoEfectivo.Fields.CipUrl.Hint"] = "Link",
                ["Plugins.Payments.PagoEfectivo.Fields.Currency"] = "Moneda",
                ["Plugins.Payments.PagoEfectivo.Fields.Currency.Hint"] = "Denominación de la Moneda",
                ["Plugins.Payments.PagoEfectivo.Fields.Amount"] = "Monto",
                ["Plugins.Payments.PagoEfectivo.Fields.Amount.Hint"] = "Monto para cobrar",
                ["Plugins.Payments.PagoEfectivo.Fields.OperationNumber"] = "Número Operación",
                ["Plugins.Payments.PagoEfectivo.Fields.OperationNumber.Hint"] = "Número de Operación de Pago Efectivo",
                ["Plugins.Payments.PagoEfectivo.Fields.TransactionCode"] = "Código Transactión",
                ["Plugins.Payments.PagoEfectivo.Fields.TransactionCode.Hint"] = "Código de Transactión de Pago Efectivo",
                ["Plugins.Payments.PagoEfectivo.Fields.PaymentDate"] = "Fecha Pago" ,
                ["Plugins.Payments.PagoEfectivo.Fields.PaymentDate.Hint"] = "Fecha de Pago",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneNumber"] = "Número de Celular",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneNumber.Hint"] = "Número de Celular para enviar sms",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneCode"] = "Código de pais",
                ["Plugins.Payments.PagoEfectivo.Fields.PhoneCode.Hint"] = "Código de pais (51 en peru)",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentType"] = "Tipo de documento",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentType.Hint"] = "Tipo de documento (DNI)",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentTypeValue"] = "Número de documento",
                ["Plugins.Payments.PagoEfectivo.Fields.DocumentTypeValue.Hint"] = "Número de documento",
                ["Plugins.Payments.PagoEfectivo.Fields.Description"] = "Descripción",
                ["Plugins.Payments.PagoEfectivo.Fields.Description.Hint"] = "Descripción del pago",
                ["Plugins.Payments.PagoEfectivo.Fields.CreatedAt"] = "Creado",
                ["Plugins.Payments.PagoEfectivo.Fields.CreatedAt.Hint"] = "Creado en",
                ["Plugins.Payments.PagoEfectivo.CipList"] = "Lista de pagos",
                ["Plugins.Payments.PagoEfectivo.AddNewCip"] = "Nuevo Cip",
                ["Plugins.Payments.PagoEfectivo.Edit"] = "Editar CIP",
                ["Plugins.Payments.PagoEfectivo.Create"] = "Crear CIP",
                ["Plugins.Payments.PagoEfectivo.BackToList"] = "volver a la lista",
                ["Plugins.Payments.PagoEfectivo.Deleted"] = "Notificación Borrada"
            };

            var languages = await _languageService.GetAllLanguagesAsync();
            foreach (var language in languages)
            {
                if ("en" == language.UniqueSeoCode)
                    await _localizationService.AddOrUpdateLocaleResourceAsync(en, language.Id);
                if ("es" == language.UniqueSeoCode)
                    await _localizationService.AddOrUpdateLocaleResourceAsync(es, language.Id);
            }

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<PagoEfectivoPaymentSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PagoEfectivo");

            //schedule task
            var task = await _scheduleTaskService.GetTaskByTypeAsync(typeof(SynchronizationTask).FullName);
            if (task is not null)
                await _scheduleTaskService.DeleteTaskAsync(task);

            await base.UninstallAsync();
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <remarks>
        /// return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
        /// for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
        /// </remarks>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.PaymentMethodDescription");
        }

        public async Task ManageSiteMapAsync(SiteMapNode rootNode)
        {
            if (!await _paymentPluginManager.IsPluginActiveAsync(PagoEfectivoDefaults.SystemName))
                return;

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return;

            var paymentNode = rootNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Configuration"));
            if (paymentNode is null)
                return;

            var paymentMethodsNode = paymentNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Payment methods"));
            if (paymentMethodsNode is null)
                return;

            paymentNode.ChildNodes.Insert(paymentNode.ChildNodes.IndexOf(paymentMethodsNode) + 1, new SiteMapNode
            {
                SystemName = PagoEfectivoDefaults.SystemNameMenu,
                Title = await _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.Menu.Title"),
                ControllerName = "PaymentPagoEfectivo",
                ActionName = "List",
                IconClass = "far fa-dot-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary { { "area", AreaNames.Admin } }
            });
        }

        #endregion
    }
}


