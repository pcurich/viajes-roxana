using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Misc.Payments.Domain.Quota;
using Nop.Plugin.Misc.Payments.Models.Configuration;
using Nop.Plugin.Misc.Payments.Models.List;
using Nop.Plugin.Misc.Payments.Models.OrderNote;
using Nop.Plugin.Misc.Payments.Models.Quota;
using Nop.Plugin.Misc.Payments.Models.QuotaDetails;
using Nop.Plugin.Misc.Payments.Services;
using Nop.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.Payments.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class CustomQuotaController : BasePluginController
    {
        #region Fields
        private readonly IPermissionService _permissionService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IOrderService _orderService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IQuotaService _quotaService;
        private readonly ISettingService _settingService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IStoreContext _storeContext;
        #endregion

        #region Ctor

        public CustomQuotaController(IPermissionService permissionService, IStoreContext storeContext, ISettingService settingService,
            INotificationService notificationService, ILocalizationService localizationService, IQuotaService quotaService,
            IDateTimeHelper dateTimeHelper, IOrderService orderService, IPriceFormatter priceFormatter, IOrderProcessingService orderProcessingService)
        {
            _permissionService = permissionService;
            _storeContext = storeContext;
            _settingService = settingService;
            _notificationService = notificationService;
            _localizationService = localizationService;
            _quotaService = quotaService;
            _dateTimeHelper = dateTimeHelper;
            _orderService = orderService;
            _priceFormatter = priceFormatter;
            _orderProcessingService = orderProcessingService;
        }

        #endregion

        #region Methods

        #region Configuration

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var customQuotaSettings = await _settingService.LoadSettingAsync<CustomQuotaSettings>(storeScope);

            var model = new ConfigurationModel
            {
                AvailableOrderStatus = await SetOrderStatusAsync(customQuotaSettings),
                AvailableFrequencies = await SetFrecuencyAsync(customQuotaSettings),

                OrderStatusId = customQuotaSettings.OrderStatusId,
                FrecuencyId = customQuotaSettings.FrecuencyId,

                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope <= 0)
                return View("~/Plugins/Misc.Payments/Views/Configure.cshtml", model);

            model.FrecuencyId_OverrideForStore = await _settingService.SettingExistsAsync(customQuotaSettings, x => x.FrecuencyId, storeScope);
            model.OrderStatusId_OverrideForStore = await _settingService.SettingExistsAsync(customQuotaSettings, x => x.OrderStatusId, storeScope);

            return View("~/Plugins/Misc.Payments/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var customQuotaSettings = await _settingService.LoadSettingAsync<CustomQuotaSettings>(storeScope);

            //save settings
            customQuotaSettings.FrecuencyId = model.FrecuencyId;
            customQuotaSettings.OrderStatusId = model.OrderStatusId;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(customQuotaSettings, x => x.FrecuencyId, model.FrecuencyId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(customQuotaSettings, x => x.OrderStatusId, model.OrderStatusId_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
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
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var customQuotaSettings = await _settingService.LoadSettingAsync<CustomQuotaSettings>(storeScope);

            var model = new QuotaSearchModel();
            model.SetGridPageSize();
            model.AvailableOrderStatus = await SetOrderStatusAsync(customQuotaSettings);
            return View("~/Plugins/Misc.Payments/Views/Quota/List.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> List(QuotaSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return await AccessDeniedDataTablesJson();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var customQuotaSettings = await _settingService.LoadSettingAsync<CustomQuotaSettings>(storeScope);

            var quotas = (await _quotaService.GetAllQuotaAsync(orderId: searchModel.OrderId, orderStatusId: searchModel.OrderStatusId,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize)).ToPagedList(searchModel);

            var model = await new QuotaListModel().PrepareToGridAsync(searchModel, quotas, () => quotas.SelectAwait(async quota =>
            {
                return new QuotaModel
                {
                    Id = quota.Id,
                    OrderId = quota.OrderId,
                    CustomerId = quota.CustomerId,
                    OrderTotal = await _priceFormatter.FormatPriceAsync(quota.OrderTotal, true, false),
                    OrderStatusId = quota.OrderStatusId,
                    OrderStatus = await _localizationService.GetLocalizedEnumAsync(quota.OrderStatus),
                    Frecuency = await _localizationService.GetLocalizedEnumAsync(quota.Frecuency),
                    StartTimeOn = await _dateTimeHelper.ConvertToUserTimeAsync(quota.StartTimeOnUtc, DateTimeKind.Utc),
                    FrecuencyId = quota.FrecuencyId,

                    AvailableFrequencies = await SetFrecuencyAsync(customQuotaSettings),

                    UpdatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(quota.UpdatedOnUtc, DateTimeKind.Utc),
                    CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(quota.CreatedOnUtc, DateTimeKind.Utc)
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
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var customQuotaSettings = await _settingService.LoadSettingAsync<CustomQuotaSettings>(storeScope);

            var model = new QuotaModel
            {
                AvailableFrequencies = await SetFrecuencyAsync(customQuotaSettings),
            };

            return View("~/Plugins/Misc.Payments/Views/Quota/Create.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual async Task<IActionResult> Create(QuotaModel model, bool continueEditing)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (model.OrderId <= 0)
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Payments.CustomQuota.Error.NoOrder"));
                return RedirectToAction("List");
            }

            var order = await _orderService.GetOrderByIdAsync(model.OrderId);
            if (order == null)
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Payments.CustomQuota.Error.NoOrder"));
                return RedirectToAction("List");
            }

            var quota = await _quotaService.GetQuotaByOrderId(model.OrderId);
            if (quota != null)
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Payments.CustomQuota.Error.QuotaExist"));
                return RedirectToAction("List");
            }

            if (model.Quotas <= 1)
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Payments.CustomQuota.Error.QuotasMoreThanOne"));
                return RedirectToAction("List");
            }

            var domain = new Quota
            {
                OrderId = model.OrderId,
                CustomerId = order.CustomerId,
                Quotas = model.Quotas,
                OrderTotal = order.OrderTotal,
                StartTimeOnUtc = _dateTimeHelper.ConvertToUtcTime(model.StartTimeOn, await _dateTimeHelper.GetCurrentTimeZoneAsync()),
                OrderStatusId = order.OrderStatusId,
                FrecuencyId = model.FrecuencyId,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await _quotaService.InsertQuotaAsync(domain);

            model.Id = domain.Id;

            for (var i = 0; i < model.Quotas; i++)
            {
                var schedulerDate = _dateTimeHelper.ConvertToUtcTime(BuildDate(model.FrecuencyId, i, model.StartTimeOn), await _dateTimeHelper.GetCurrentTimeZoneAsync());
                var priceWithFormat = await _priceFormatter.FormatPriceAsync(Math.Round(order.OrderTotal / model.Quotas, 2), true, false);
                var template = await _localizationService.GetResourceAsync("Plugins.Payments.CustomQuota.Order.Note");

                var orderNote = new OrderNote
                {
                    OrderId = order.Id,
                    DisplayToCustomer = true,
                    DownloadId = 0,
                    CreatedOnUtc = DateTime.UtcNow
                };

                orderNote.Note = "----------" + await _dateTimeHelper.ConvertToUserTimeAsync(DateTime.UtcNow, DateTimeKind.Utc) + "----------" + Environment.NewLine;
                orderNote.Note += string.Format(template, i + 1, schedulerDate, priceWithFormat, await _localizationService.GetLocalizedEnumAsync(PaymentStatus.Pending));
                orderNote.Note += Environment.NewLine;

                await _orderService.InsertOrderNoteAsync(orderNote);
                var quotaDetail = new QuotaDetails
                {
                    QuotaId = domain.Id,
                    OrderId = order.Id,
                    OrderNoteId = orderNote.Id,
                    CustomerId = order.CustomerId,
                    Sequence = i + 1,
                    Amount = Math.Round(order.OrderTotal / model.Quotas, 2),
                    CurrencyCode = order.CustomerCurrencyCode,
                    PaymentStatusId = (int)PaymentStatus.Pending,
                    SchedulerDateOnUtc = schedulerDate,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow
                };

                await _quotaService.InsertQuotaDetailsAsync(quotaDetail);
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
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var customQuotaSettings = await _settingService.LoadSettingAsync<CustomQuotaSettings>(storeScope);

            //try to get a category with the specified id
            var quota = await _quotaService.GetQuotaByIdAsync(id);
            if (quota == null)
                return RedirectToAction("~/Plugins/Misc.Payments/Views/Quota/List.cshtml");

            //prepare model
            var model = new QuotaModel
            {
                Id = quota.Id,
                OrderId = quota.OrderId,
                CustomerId = quota.CustomerId,
                OrderTotal = await _priceFormatter.FormatPriceAsync(quota.OrderTotal, true, false),
                OrderStatusId = quota.OrderStatusId,
                OrderStatus = await _localizationService.GetLocalizedEnumAsync(quota.OrderStatus),
                FrecuencyId = quota.FrecuencyId,
                Frecuency = await _localizationService.GetLocalizedEnumAsync(quota.Frecuency),
                Quotas = quota.Quotas,
                StartTimeOn = await _dateTimeHelper.ConvertToUserTimeAsync(quota.StartTimeOnUtc, DateTimeKind.Utc),
                UpdatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(quota.UpdatedOnUtc, DateTimeKind.Utc),
                CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(quota.CreatedOnUtc, DateTimeKind.Utc),

                AvailableFrequencies = await SetFrecuencyAsync(customQuotaSettings)

            };

            return View("~/Plugins/Misc.Payments/Views/Quota/Edit.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> Delete(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //try to get a category with the specified id
            var quota = await _quotaService.GetQuotaByIdAsync(id);
            if (quota == null)
                return RedirectToAction("List");

            var quotaDetails = await _quotaService.GetAllQuotaDetailsAsync(id);
            foreach (var q in quotaDetails)
            {
                var orderNote = await _orderService.GetOrderNoteByIdAsync(q.OrderNoteId);
                await _orderService.DeleteOrderNoteAsync(orderNote);
            }

            await _quotaService.DeleteQuotaAsync(quota);
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Payments.PagoEfectivo.Deleted"));

            return RedirectToAction("List");
        }

        #endregion

        #region ListDetails

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> ListDetails(QuotaDetailsSearchModel searchModel)
        {

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return await AccessDeniedDataTablesJson();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var customQuotaSettings = await _settingService.LoadSettingAsync<CustomQuotaSettings>(storeScope);

            var quotas = (await _quotaService.GetAllQuotaDetailsAsync(searchModel.QuotaId)).ToPagedList(searchModel);

            var model = await new QuotaDetailsListModel().PrepareToGridAsync(searchModel, quotas, () => quotas.SelectAwait(async quota =>
            {
                return new QuotaDetailsModel
                {
                    Id = quota.Id,
                    OrderId = quota.OrderId,
                    QuotaId = quota.QuotaId,
                    OrderNoteId = quota.OrderNoteId,
                    CustomerId = quota.CustomerId,
                    Sequence = quota.Sequence,
                    Amount = quota.Amount,
                    AmountStr = await _priceFormatter.FormatPriceAsync(quota.Amount, true, false),
                    PaymentStatusId = quota.PaymentStatusId,
                    PaymentStatus = await _localizationService.GetLocalizedEnumAsync(quota.PaymentStatus),
                    SchedulerDateOn = await _dateTimeHelper.ConvertToUserTimeAsync(quota.SchedulerDateOnUtc, DateTimeKind.Utc),
                    PaymentDateOn = quota.PaymentDateOnUtc.HasValue ? await _dateTimeHelper.ConvertToUserTimeAsync(quota.PaymentDateOnUtc.Value, DateTimeKind.Utc) : null,

                    UpdatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(quota.UpdatedOnUtc, DateTimeKind.Utc),
                    CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(quota.CreatedOnUtc, DateTimeKind.Utc)
                };
            }));

            return Json(model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpGet]
        public virtual async Task<IActionResult> EditDetails(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //try to get a category with the specified id
            var quotaDetail = await _quotaService.GetQuotaDetailsByIdAsync(id);
            if (quotaDetail == null)
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Payments.CustomQuota.Error.NoQuotaDetailOrder"));
                return RedirectToAction("Edit", new { id = quotaDetail.QuotaId });
            }

            if (quotaDetail.PaymentStatusId == (int)PaymentStatus.Paid)
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Payments.CustomQuota.Error.AsPayed"));
                return RedirectToAction("Edit", new { id = quotaDetail.QuotaId });
            }

            var note = await _orderService.GetOrderNoteByIdAsync(quotaDetail.OrderNoteId);

            //prepare model
            var model = new QuotaDetailsModel
            {
                Id = quotaDetail.Id,
                QuotaId = quotaDetail.QuotaId,
                OrderId = quotaDetail.OrderId,
                OrderNoteId = quotaDetail.OrderNoteId,
                CustomerId = quotaDetail.CustomerId,
                Sequence = quotaDetail.Sequence,
                Amount = quotaDetail.Amount,
                AmountStr = await _priceFormatter.FormatPriceAsync(quotaDetail.Amount, true, false),
                PaymentStatusId = quotaDetail.PaymentStatusId,
                PaymentStatus = await _localizationService.GetLocalizedEnumAsync(quotaDetail.PaymentStatus),
                SchedulerDateOn = await _dateTimeHelper.ConvertToUserTimeAsync(quotaDetail.SchedulerDateOnUtc, DateTimeKind.Utc),
                PaymentDateOn = quotaDetail.PaymentDateOnUtc.HasValue ? await _dateTimeHelper.ConvertToUserTimeAsync(quotaDetail.PaymentDateOnUtc.Value, DateTimeKind.Utc) : null,
                UpdatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(quotaDetail.UpdatedOnUtc, DateTimeKind.Utc),
                CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(quotaDetail.CreatedOnUtc, DateTimeKind.Utc),
                AvailablePaymentStatus = await SetPaymentStatusAsync(quotaDetail.PaymentStatusId),
                OrderNoteModel = new OrderNoteModel
                {
                    OrderNoteId = note.Id,
                    AddOrderNoteDisplayToCustomer = note.DisplayToCustomer,
                    AddOrderNoteDownloadId = note.DownloadId
                }
            };

            return View("~/Plugins/Misc.Payments/Views/QuotaDetails/Edit.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual async Task<IActionResult> EditDetails(QuotaDetailsModel model, bool continueEditing)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            var quotaDetails = await _quotaService.GetQuotaDetailsByIdAsync(model.Id);
            quotaDetails.Amount = model.Amount;
            quotaDetails.SchedulerDateOnUtc = _dateTimeHelper.ConvertToUtcTime(model.SchedulerDateOn, await _dateTimeHelper.GetCurrentTimeZoneAsync());
            quotaDetails.PaymentStatusId = model.PaymentStatusId;

            if (model.PaymentStatusId == (int)PaymentStatus.Paid)
            {
                #region closeQuota

                quotaDetails.PaymentDateOnUtc = DateTime.UtcNow;
                var quotas = await _quotaService.GetAllQuotaDetailsAsync(quotaDetails.QuotaId);
                var quota = await _quotaService.GetQuotaByIdAsync(quotaDetails.QuotaId);
                var subTotal = new decimal();
                foreach (var q in quotas)
                {
                    if (q.PaymentStatusId == (int)PaymentStatus.Paid)
                    {
                        subTotal += q.Amount;
                    }
                }
                subTotal += model.Amount;
                if (quota.OrderTotal == subTotal)
                {
                    quota.OrderStatusId = (int)OrderStatus.Complete;
                    await _quotaService.UpdateQuotaAsync(quota);
                    var order = await _orderService.GetOrderByIdAsync(quotaDetails.OrderId);
                    await _orderProcessingService.MarkOrderAsPaidAsync(order);

                }

                #endregion
            }

            var orderNote = await _orderService.GetOrderNoteByIdAsync(quotaDetails.OrderNoteId);
            var schedulerDate = await _dateTimeHelper.ConvertToUserTimeAsync(quotaDetails.SchedulerDateOnUtc, DateTimeKind.Utc);
            var priceWithFormat = await _priceFormatter.FormatPriceAsync(quotaDetails.Amount, true, false);
            var template = await _localizationService.GetResourceAsync("Plugins.Payments.CustomQuota.Order.Note");

            var oldNote = orderNote.Note.Split(Environment.NewLine);
            orderNote.Note = "----------" + await _dateTimeHelper.ConvertToUserTimeAsync(DateTime.UtcNow, DateTimeKind.Utc) + "----------" + Environment.NewLine;
            orderNote.Note += string.Format(template, quotaDetails.Sequence, schedulerDate, priceWithFormat, await _localizationService.GetLocalizedEnumAsync(quotaDetails.PaymentStatusId == (int)PaymentStatus.Paid ? PaymentStatus.Paid : PaymentStatus.Pending));
            orderNote.Note += Environment.NewLine;

            if (oldNote.Length > 1)
            {
                for (var i = 2; i < oldNote.Length; i++)
                {
                    if (oldNote[i].Length > 0)
                    {
                        if (oldNote[i] == Environment.NewLine)
                            orderNote.Note += oldNote[i];
                        else
                            orderNote.Note += oldNote[i] + Environment.NewLine;
                    }
                    
                }
            }
            orderNote.Note += Environment.NewLine;

            await _quotaService.UpdateQuotaDetailsAsync(quotaDetails);
            await _quotaService.UpdateOrderNoteAsync(orderNote);

            if (model.PaymentStatusId == (int)PaymentStatus.Paid)
                return RedirectToAction("Edit", new { id = quotaDetails.QuotaId });

            if (!continueEditing)
                return RedirectToAction("Edit", new { id = quotaDetails.QuotaId });

            return RedirectToAction("EditDetails", new { id = model.Id });
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        public virtual async Task<IActionResult> OrderNoteEdit(int orderNoteId, int downloadId, bool displayToCustomer, string message)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            if (string.IsNullOrEmpty(message))
                return ErrorJson(await _localizationService.GetResourceAsync("Admin.Orders.OrderNotes.Fields.Note.Validation"));

            //try to get an order with the specified id
            var note = await _orderService.GetOrderNoteByIdAsync(orderNoteId);
            if (note == null)
                return ErrorJson("Order Note cannot be loaded");

            note.Note += "----------" + await _dateTimeHelper.ConvertToUserTimeAsync(DateTime.UtcNow, DateTimeKind.Utc) + "----------" + Environment.NewLine;
            note.Note += message;
            note.Note += Environment.NewLine;

            note.DisplayToCustomer = displayToCustomer;
            note.DownloadId = downloadId;
            await _quotaService.UpdateOrderNoteAsync(note);

            return Json(new { Result = true });
        }

        #endregion

        #region Util

        public static async Task<IList<SelectListItem>> SetOrderStatusAsync(CustomQuotaSettings setting, bool addClean = true)
        {
            var data = (await OrderStatus.Pending.ToSelectListAsync(useLocalization: true))?.Select(account => new SelectListItem
            {
                Value = account.Value,
                Text = account.Text,
                Selected = setting.OrderStatusId.ToString().Contains(account.Value)
            }).ToList() ?? new();

            if (addClean)
                data.Insert(0, new SelectListItem { Selected = false, Text = "-------------------", Value = "0" });


            return data;
        }

        public static async Task<IList<SelectListItem>> SetFrecuencyAsync(CustomQuotaSettings setting)
        {
            return (await FrecuencyList.Annual.ToSelectListAsync(useLocalization: true))?.Select(account => new SelectListItem
            {
                Value = account.Value,
                Text = account.Text,
                Selected = setting.FrecuencyId.ToString().Contains(account.Value)
            }).ToList() ?? new();
        }

        public static async Task<IList<SelectListItem>> SetPaymentStatusAsync(int paymentStatusId)
        {
            return (await PaymentStatus.Pending.
                ToSelectListAsync(
                    useLocalization: true,
                    valuesToExclude: new[] {
                        (int)PaymentStatus.Authorized,
                        (int)PaymentStatus.Refunded,
                        (int)PaymentStatus.PartiallyRefunded,
                        (int)PaymentStatus.Voided
                    }))?.
                Select(a => new SelectListItem
                {
                    Value = a.Value,
                    Text = a.Text,
                    Selected = int.Parse(a.Value) == paymentStatusId
                }).ToList() ?? new();
        }


        public static DateTime BuildDate(int frecuencyId, int step, DateTime startDate)
        {
            return frecuencyId switch
            {
                (int)FrecuencyList.Daily => startDate.AddDays(step * 1),
                (int)FrecuencyList.Weekly => startDate.AddDays(step * 7),
                (int)FrecuencyList.Fortnightly => startDate.AddDays(step * 15),
                (int)FrecuencyList.Monthly => startDate.AddMonths(step * 1),
                (int)FrecuencyList.Bimonthly => startDate.AddMonths(step * 2),
                (int)FrecuencyList.Quarterly => startDate.AddMonths(step * 3),
                (int)FrecuencyList.Semestral => startDate.AddMonths(step * 6),
                (int)FrecuencyList.Annual => startDate.AddMonths(step * 12),
                _ => DateTime.Now,
            };
        }

        #endregion


        #endregion Methods
    }
}
