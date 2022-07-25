using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Orders;
using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework.Controllers;
using Nop.Services.Logging;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework;
using Nop.Plugin.Payments.PagoEfectivo.Models;
using System.IO;
using Nop.Plugin.Payments.PagoEfectivo.Services;
using Nop.Plugin.Payments.PagoEfectivo.Domain;
using Nop.Web.Framework.Models.Extensions;
using System.Linq;
using System;
using System.Collections.Generic;
using Nop.Services.Helpers;

namespace Nop.Plugin.Payments.PagoEfectivo.Controllers
{

    public class PaymentPagoEfectivoController : BasePaymentController
    {
        #region Fields

        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly INotificationPagoEfectivoService _notificationPagoEfectivoService;
        private readonly PagoEfectivoHttpClient _pagoEfectivoHttpClient;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly IDateTimeHelper _dateTimeHelper;

        #endregion

        #region Ctor

        public PaymentPagoEfectivoController(
            IOrderService orderService,
            IPaymentService paymentService,
            IPermissionService permissionService,
            ILocalizationService localizationService,
            ILogger logger,
            INotificationService notificationService,
            ISettingService settingService,
            IStoreContext storeContext,
            IWebHelper webHelper,
            IDateTimeHelper dateTimeHelper,
            IWorkContext workContext,
            INotificationPagoEfectivoService notificationPagoEfectivoService,
            PagoEfectivoHttpClient pagoEfectivoHttpClient,
            ShoppingCartSettings shoppingCartSettings)
        {
            _orderService = orderService;
            _paymentService = paymentService;
            _permissionService = permissionService;
            _localizationService = localizationService;
            _logger = logger;
            _dateTimeHelper = dateTimeHelper;
            _notificationService = notificationService;
            _settingService = settingService;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _workContext = workContext;
            _notificationPagoEfectivoService = notificationPagoEfectivoService;
            _shoppingCartSettings = shoppingCartSettings;
            _pagoEfectivoHttpClient = pagoEfectivoHttpClient;
        }

        #endregion

        #region Methods

        #region Configuration

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> ConfigureAsync()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var pagoEfectivoPaymentSettings = await _settingService.LoadSettingAsync<PagoEfectivoPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                UseSandbox = pagoEfectivoPaymentSettings.UseSandbox,
                ApiDev = pagoEfectivoPaymentSettings.ApiDev,
                ApiPrd = pagoEfectivoPaymentSettings.ApiPrd,
                UrlAuthorizationToCip = pagoEfectivoPaymentSettings.UrlAuthorizationToCip,
                UrlCip = pagoEfectivoPaymentSettings.UrlCip,
                AccessKey = pagoEfectivoPaymentSettings.AccessKey,
                IdService = pagoEfectivoPaymentSettings.IdService,
                SecretKey = pagoEfectivoPaymentSettings.SecretKey,

                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope <= 0)
                return View("~/Plugins/Payments.PagoEfectivo/Views/Configure.cshtml", model);

            model.UseSandbox_OverrideForStore = await _settingService.SettingExistsAsync(pagoEfectivoPaymentSettings, x => x.UseSandbox, storeScope);
            model.ApiDev_OverrideForStore = await _settingService.SettingExistsAsync(pagoEfectivoPaymentSettings, x => x.ApiDev, storeScope);
            model.ApiPrd_OverrideForStore = await _settingService.SettingExistsAsync(pagoEfectivoPaymentSettings, x => x.ApiPrd, storeScope);
            model.UrlAuthorizationToCip_OverrideForStore = await _settingService.SettingExistsAsync(pagoEfectivoPaymentSettings, x => x.UrlAuthorizationToCip, storeScope);
            model.UrlCip_OverrideForStore = await _settingService.SettingExistsAsync(pagoEfectivoPaymentSettings, x => x.UrlCip, storeScope);
            model.AccessKey_OverrideForStore = await _settingService.SettingExistsAsync(pagoEfectivoPaymentSettings, x => x.AccessKey, storeScope);
            model.IdService_OverrideForStore = await _settingService.SettingExistsAsync(pagoEfectivoPaymentSettings, x => x.IdService, storeScope);
            model.SecretKey_OverrideForStore = await _settingService.SettingExistsAsync(pagoEfectivoPaymentSettings, x => x.SecretKey, storeScope);

            return View("~/Plugins/Payments.PagoEfectivo/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> ConfigureAsync(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await ConfigureAsync();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var pagoEfectivoPaymentSettings = await _settingService.LoadSettingAsync<PagoEfectivoPaymentSettings>(storeScope);

            //save settings
            pagoEfectivoPaymentSettings.UseSandbox = model.UseSandbox;
            pagoEfectivoPaymentSettings.AccessKey = model.AccessKey;
            pagoEfectivoPaymentSettings.IdService = model.IdService;
            pagoEfectivoPaymentSettings.SecretKey = model.SecretKey;
            pagoEfectivoPaymentSettings.ApiDev = model.ApiDev;
            pagoEfectivoPaymentSettings.ApiPrd = model.ApiPrd;
            pagoEfectivoPaymentSettings.UrlAuthorizationToCip = model.UrlAuthorizationToCip;
            pagoEfectivoPaymentSettings.UrlCip = model.UrlCip;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(pagoEfectivoPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(pagoEfectivoPaymentSettings, x => x.AccessKey, model.AccessKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(pagoEfectivoPaymentSettings, x => x.IdService, model.IdService_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(pagoEfectivoPaymentSettings, x => x.SecretKey, model.SecretKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(pagoEfectivoPaymentSettings, x => x.ApiDev, model.ApiDev_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(pagoEfectivoPaymentSettings, x => x.ApiPrd, model.ApiPrd_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(pagoEfectivoPaymentSettings, x => x.UrlAuthorizationToCip, model.UrlAuthorizationToCip_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(pagoEfectivoPaymentSettings, x => x.UrlCip, model.UrlCip_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await ConfigureAsync();
        }

        #endregion

        #region List

        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> List()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var model = new NotificationPagoEfectivoSearchModel();
            model.SetGridPageSize();
            return View("~/Plugins/Payments.PagoEfectivo/Views/List.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> List(NotificationPagoEfectivoSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return await AccessDeniedDataTablesJson();

            var notifications = (await _notificationPagoEfectivoService.GetAllNotificationRequestAsync(0, searchModel.OrderId, searchModel.Cip)).ToPagedList(searchModel);
            var model = await new NotificationPagoEfectivoListModel().PrepareToGridAsync(searchModel, notifications, () => notifications.SelectAwait(async notification =>
            {
                return new NotificationPagoEfectivoModel
                {
                    Id = notification.Id,
                    OrderId = notification.OrderId,
                    OperationNumber = notification.OperationNumber,
                    Cip = notification.Cip,
                    Currency = notification.Currency,
                    Amount = notification.Amount,
                    PaymentDate = notification.PaymentDate,
                    TransactionCode = notification.TransactionCode,
                    CreatedAt = await _dateTimeHelper.ConvertToUserTimeAsync(notification.CreatedAt, DateTimeKind.Utc)
                };
            }));

            return Json(model);
        }

        #endregion

        #region Create / Edit / Delete

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var model = new NotificationPagoEfectivoModel
            {
                PhoneCode = PagoEfectivoDefaults.DefaultPhoneCode,
                Description = PagoEfectivoDefaults.DefaultDescription,
                DocumentType = PagoEfectivoDefaults.DefaultDocumentType,
                Currency = PagoEfectivoDefaults.DefaultCurrency
            };

            return View("~/Plugins/Payments.PagoEfectivo/Views/Create.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual async Task<IActionResult> Create(NotificationPagoEfectivoModel model, bool continueEditing)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var pagoEfectivoPaymentSettings = await _pagoEfectivoHttpClient.AuthorizationsAsync();
                var order = new Order { CustomerCurrencyCode = model.Currency, OrderTotal = model.Amount };
                var customValues = new Dictionary<string, object>
                {
                    { _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.DocumentType").Result, model.DocumentType },
                    { _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.DocumentTypeValue").Result, model.DocumentTypeValue },
                    { _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.PhoneNumber").Result, model.PhoneNumber },
                    { _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.PhoneCode").Result, model.PhoneCode }
                };

                order.CustomValuesXml = _paymentService.SerializeCustomValues(new ProcessPaymentRequest { CustomValues = customValues });
                model.Id = await BuildNotificationAsync(order, model.Description, model, pagoEfectivoPaymentSettings);

                #region Extra
                if (model.Id == 0)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        model.Id = await BuildNotificationAsync(order, model.Description, model, pagoEfectivoPaymentSettings);
                        if (model.Id > 0)
                            break;
                    }
                }
                #endregion
            }

            if (!continueEditing)
                return RedirectToAction("List");

            return RedirectToAction("Edit", new { id = model.Id });
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpGet]
        public virtual async Task<IActionResult> Edit(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCategories))
                return AccessDeniedView();

            //try to get a category with the specified id
            var notificationPagoEfectivo = await _notificationPagoEfectivoService.GetAllNotificationRequestAsync(id);
            if (notificationPagoEfectivo == null)
                return RedirectToAction("~/Plugins/Payments.PagoEfectivo/Views/List.cshtml");

            //prepare model
            var notification = notificationPagoEfectivo.FirstOrDefault();
            var model = new NotificationPagoEfectivoModel
            {
                Id = notification.Id,
                OrderId = notification.OrderId,
                Cip = notification.Cip,
                CipUrl = notification.CipUrl,
                OperationNumber = notification.OperationNumber,
                TransactionCode = notification.TransactionCode,
                Currency = notification.Currency,
                Amount = notification.Amount,
                PaymentDate = notification.PaymentDate,
                DocumentType = notification.DocumentType,
                DocumentTypeValue = notification.DocumentTypeValue,
                PhoneCode = notification.PhoneCode,
                PhoneNumber = notification.PhoneNumber,
                Description = notification.Description,
                CreatedAt = await _dateTimeHelper.ConvertToUserTimeAsync(notification.CreatedAt, DateTimeKind.Utc)
            };

            return View("~/Plugins/Payments.PagoEfectivo/Views/Edit.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual async Task<IActionResult> Edit(NotificationPagoEfectivoModel model, bool continueEditing)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var pagoEfectivoPaymentSettings = await _pagoEfectivoHttpClient.AuthorizationsAsync();

                var orderItems = await _orderService.GetOrderItemsAsync(model.OrderId);
                var order = await _orderService.GetOrderByIdAsync(model.OrderId);
                var productName = "";

                foreach (var orderItem in orderItems)
                {
                    var product = _orderService.GetProductByOrderItemIdAsync(orderItem.Id).Result;
                    productName = productName + "|" + product.Name;
                }

                productName = productName.Substring(1, productName.Length - 1);
                model.Id = await BuildNotificationAsync(order, productName, model, pagoEfectivoPaymentSettings);

                #region Extra
                if (model.Id == 0)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        model.Id = await BuildNotificationAsync(order, model.Description, model, pagoEfectivoPaymentSettings);
                        if (model.Id > 0)
                            break;
                    }
                }
                #endregion 
            }
            if (!continueEditing)
                return RedirectToAction("List");

            return RedirectToAction("Edit", new { id = model.Id });
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> Delete(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCategories))
                return AccessDeniedView();

            //try to get a category with the specified id
            var notification = await _notificationPagoEfectivoService.GetNotificationRequestByIdAsync(id);
            if (notification == null)
                return RedirectToAction("List");

            await _notificationPagoEfectivoService.DeleteNotificationRequestAsync(notification);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.Deleted"));

            return RedirectToAction("List");
        }

        #endregion

        #region IPN

        [HttpPost]
        public IActionResult AutomaticNotification()
        {
            using var stream = new MemoryStream();
            Request.Body.CopyToAsync(stream);
            string signature = Request.Headers["PE-Signature"];
            var strRequest = Encoding.ASCII.GetString(stream.ToArray());

            var storeScope = _storeContext.GetActiveStoreScopeConfigurationAsync().Result;
            var pagoEfectivoPaymentSettings = _settingService.LoadSettingAsync<PagoEfectivoPaymentSettings>(storeScope);

            (var cip, var currency, var amount, var paymentDate, var transactionCode) = PagoEfectivoHttpClient.GetCipPayment(strRequest);

            var notification = _notificationPagoEfectivoService.GetNotificationRequestByCipAsync(cip).Result;

            if (notification != null)
            {
                notification.Amount = amount;
                notification.Cip = cip;
                notification.Currency = currency;
                notification.PaymentDate = paymentDate;
                notification.TransactionCode = transactionCode;
                notification.Signature = signature;
                _notificationPagoEfectivoService.UpdateNotificationRequestAsync(notification);
            }
            else
            {
                //No es normal este caso
                _notificationPagoEfectivoService.InsertNotificationRequestAsync(new NotificationPagoEfectivo
                {
                    Cip = cip,
                    Amount = amount,
                    Currency = currency,
                    PaymentDate = paymentDate,
                    TransactionCode = transactionCode
                });
            }

            return Ok();
        }

        #endregion


        #endregion Methods

        #region Util

        public async Task<int> BuildNotificationAsync(Order order, string productName, NotificationPagoEfectivoModel model, PagoEfectivoPaymentSettings pagoEfectivoPaymentSettings)
        {
            var (cipUrl, cip, transactionCode, code, message) = await _pagoEfectivoHttpClient.GetCipAsync(order, productName);
            if (cip != "0")
            {
                if (model.OrderId > 0)
                {
                    #region UpdateOrder
                    await _orderService.InsertOrderNoteAsync(new OrderNote { OrderId = order.Id, Note = cip.ToString(), DisplayToCustomer = true, CreatedOnUtc = DateTime.UtcNow });
                    await _orderService.InsertOrderNoteAsync(new OrderNote { OrderId = order.Id, Note = cipUrl, DisplayToCustomer = true, CreatedOnUtc = DateTime.UtcNow });

                    order.AuthorizationTransactionResult = cipUrl;
                    order.AuthorizationTransactionId = cip;
                    order.AuthorizationTransactionCode = transactionCode;
                    order.CaptureTransactionResult = message;
                    order.CaptureTransactionId = code;
                    order.OrderStatus = OrderStatus.Pending;

                    await _orderService.UpdateOrderAsync(order);
                    #endregion
                }

                var notificationPagoEfectivo = await _notificationPagoEfectivoService.GetNotificationRequestByIdAsync(model.Id);

                if (notificationPagoEfectivo == null)
                {
                    notificationPagoEfectivo = new NotificationPagoEfectivo
                    {
                        OrderId = order.Id,
                        Type = PagoEfectivoDefaults.DefaultManual,
                        Cip = cip,
                        CipUrl = cipUrl,
                        OperationNumber = code,
                        TransactionCode = transactionCode,
                        Currency = model.Currency,
                        Amount = model.Amount,
                        PaymentDate = model.PaymentDate,
                        Token = pagoEfectivoPaymentSettings.Token,
                        TokenExpires = pagoEfectivoPaymentSettings.TokenExpires,
                        TokenStart = pagoEfectivoPaymentSettings.TokenStart,
                        DocumentType = model.DocumentType,
                        DocumentTypeValue = model.DocumentTypeValue,
                        PhoneCode = model.PhoneCode,
                        PhoneNumber = model.PhoneNumber,
                        Description = model.Description,
                        CreatedAt = DateTime.UtcNow,
                        Signature = message
                    };
                    await _notificationPagoEfectivoService.InsertNotificationRequestAsync(notificationPagoEfectivo);
                }
                else
                {
                    notificationPagoEfectivo.Type = PagoEfectivoDefaults.DefaultManual;
                    notificationPagoEfectivo.Cip = cip;
                    notificationPagoEfectivo.CipUrl = cipUrl;
                    notificationPagoEfectivo.OperationNumber = code;
                    notificationPagoEfectivo.TransactionCode = transactionCode;

                    notificationPagoEfectivo.PaymentDate = null;
                    notificationPagoEfectivo.Token = pagoEfectivoPaymentSettings.Token;
                    notificationPagoEfectivo.TokenExpires = pagoEfectivoPaymentSettings.TokenExpires;
                    notificationPagoEfectivo.TokenStart = pagoEfectivoPaymentSettings.TokenStart;
                    notificationPagoEfectivo.DocumentType = model.DocumentType;
                    notificationPagoEfectivo.DocumentTypeValue = model.DocumentTypeValue;
                    notificationPagoEfectivo.PhoneCode = model.PhoneCode;
                    notificationPagoEfectivo.PhoneNumber = model.PhoneNumber;
                    notificationPagoEfectivo.Description = model.Description;
                    notificationPagoEfectivo.CreatedAt = DateTime.UtcNow;
                    notificationPagoEfectivo.Signature = message;

                    await _notificationPagoEfectivoService.UpdateNotificationRequestAsync(notificationPagoEfectivo);
                }


                return notificationPagoEfectivo.Id;
            }
            return 0;
        }

        #endregion

    }
}
