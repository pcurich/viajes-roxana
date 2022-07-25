using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.Izipay.Models;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Izipay.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class PaymentIzipayController : BasePaymentController 
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public PaymentIzipayController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var izipayPaymentSettings = await _settingService.LoadSettingAsync<IzipayPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                UseSandbox = izipayPaymentSettings.UseSandbox,
                UrlCreateToken = izipayPaymentSettings.UrlCreateToken,
                User = izipayPaymentSettings.User,
                DevPassword = izipayPaymentSettings.DevPassword,
                ProdPassword = izipayPaymentSettings.ProdPassword,

                TransactModeId = Convert.ToInt32(izipayPaymentSettings.TransactMode),
                AdditionalFee = izipayPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = izipayPaymentSettings.AdditionalFeePercentage,
                TransactModeValues = await izipayPaymentSettings.TransactMode.ToSelectListAsync(),
                ActiveStoreScopeConfiguration = storeScope
            };
            if (storeScope > 0)
            {
                model.TransactModeId_OverrideForStore = await _settingService.SettingExistsAsync(manualPaymentSettings, x => x.TransactMode, storeScope);
                model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(manualPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(manualPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.Manual/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var manualPaymentSettings = await _settingService.LoadSettingAsync<ManualPaymentSettings>(storeScope);

            //save settings
            manualPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;
            manualPaymentSettings.AdditionalFee = model.AdditionalFee;
            manualPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */

            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.TransactMode, model.TransactModeId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #endregion
    }
}
