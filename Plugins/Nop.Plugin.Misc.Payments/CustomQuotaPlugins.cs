using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Plugin.Misc.Payments.Models.List;
using Nop.Plugin.Misc.Payments.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Misc.Payments
{
    public class CustomQuotaPlugins : BasePlugin, IMiscPlugin, IAdminMenuPlugin
    {

        #region Fields
        private readonly ILocalizationService _localizationService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILanguageService _languageService;
        private readonly IPermissionService _permissionService;
        #endregion

        #region Ctor
        public CustomQuotaPlugins(IPermissionService permissionService, ILanguageService languageService, IScheduleTaskService scheduleTaskService, ISettingService settingService,
            ILocalizationService localizationService, IWebHelper webHelper)
        {
            _permissionService = permissionService;
            _languageService = languageService;
            _scheduleTaskService = scheduleTaskService;
            _settingService = settingService;
            _localizationService = localizationService;
            _webHelper = webHelper;
        }
        #endregion

        #region Methods

        public async Task ManageSiteMapAsync(SiteMapNode rootNode)
        {

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return;

            var paymentNode = rootNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Configuration"));
            if (paymentNode is null)
                return;

            var paymentMethodsNode = paymentNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Payment methods"));
            if (paymentMethodsNode is null)
                return;

            paymentNode.ChildNodes.Insert(paymentNode.ChildNodes.IndexOf(paymentMethodsNode) + 2, new SiteMapNode
            {
                SystemName = CustomQuotaDefaults.SystemNameMenu,
                Title = await _localizationService.GetResourceAsync("Plugins.Payments.CustomQuota.Menu.Title"),
                ControllerName = "CustomQuota",
                ActionName = "List",
                IconClass = "far fa-dot-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary { { "area", AreaNames.Admin } }
            });
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/CustomQuota/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            //settings
            await _settingService.SaveSettingAsync(new CustomQuotaSettings());

            //schedule task
            //if (await _scheduleTaskService.GetTaskByTypeAsync(typeof(CreateCustomQuotaTask).FullName) is null)
            //{
            //    await _scheduleTaskService.InsertTaskAsync(new()
            //    {
            //        Enabled = true,
            //        LastEnabledUtc = DateTime.UtcNow,
            //        StopOnError = false,
            //        Seconds = 60 * 60,
            //        Name = CustomQuotaDefaults.SystemNameSynchronization,
            //        Type = typeof(SynchronizationTask).FullName
            //    });
            //}

            //locales
            var en = new Dictionary<string, string>
            {
                ["Plugins.Payments.CustomQuota.Fields.OrderId"] = "Order Id",
                ["Plugins.Payments.CustomQuota.Fields.OrderId.Hint"] = "Order Id",
                ["Plugins.Payments.CustomQuota.Fields.CustomerId"] = "Customer Id",
                ["Plugins.Payments.CustomQuota.Fields.CustomerId.Hint"] = "Customer Id",
                ["Plugins.Payments.CustomQuota.Fields.Quotas"] = "# Quotas",
                ["Plugins.Payments.CustomQuota.Fields.Quotas.Hint"] = "# Quotas",
                ["Plugins.Payments.CustomQuota.Fields.OrderNoteId"] = "Order Note Id",
                ["Plugins.Payments.CustomQuota.Fields.OrderNoteId.Hint"] = "Order Note Id",
                ["Plugins.Payments.CustomQuota.Fields.Sequence"] = "Sequence",
                ["Plugins.Payments.CustomQuota.Fields.Sequence.Hint"] = "Sequence",
                ["Plugins.Payments.CustomQuota.Fields.Amount"] = "Amount",
                ["Plugins.Payments.CustomQuota.Fields.Amount.Hint"] = "Amount",
                ["Plugins.Payments.CustomQuota.Fields.OrderTotal"] = "Amount Total",
                ["Plugins.Payments.CustomQuota.Fields.OrderTotal.Hint"] = "Amount  Total",
                ["Plugins.Payments.CustomQuota.Fields.PaymentStatusId"] = "Payment Status",
                ["Plugins.Payments.CustomQuota.Fields.PaymentStatusId.Hint"] = "Payment Status",
                ["Plugins.Payments.CustomQuota.Fields.SchedulerDateOn"] = "Scheduler Date",
                ["Plugins.Payments.CustomQuota.Fields.SchedulerDateOn.Hint"] = "Scheduler Date",
                ["Plugins.Payments.CustomQuota.Fields.PaymentDateOn"] = "Payment Date",
                ["Plugins.Payments.CustomQuota.Fields.PaymentDateOn.Hint"] = "Payment Date",
                ["Plugins.Payments.CustomQuota.Fields.CreatedOn"] = "Created on",
                ["Plugins.Payments.CustomQuota.Fields.CreatedOn.Hint"] = "Date of creation",
                ["Plugins.Payments.CustomQuota.Fields.UpdatedOn"] = "Updated on",
                ["Plugins.Payments.CustomQuota.Fields.UpdatedOn.Hint"] = "Date of last updated",

                ["Plugins.Payments.CustomQuota.Menu.Title"] = "Create Quotas",
                ["Plugins.Payments.CustomQuota.Error.NoOrder"] = "The order code does not exist, enter the correct code again",
                ["Plugins.Payments.CustomQuota.Error.NoQuotaDetailOrder"] = "There is no informationn",
                ["Plugins.Payments.CustomQuota.Error.AsPayed"] = "Can't edit as it's paid",
                ["Plugins.Payments.CustomQuota.Error.QuotaExist"] = "It cannot be divided into installments because this order already has installments",
                ["Plugins.Payments.CustomQuota.Error.QuotasMoreThanOne"] = "The number of installments must be greater than 1",

                ["Plugins.Payments.CustomQuota.Deleted"] = "Quotas Deleted",
                ["Plugins.Payments.CustomQuota.Order.Note"] = "# Quota: {0} Scheduler Date: {1} Amount to paid: {2} State: {3}",

                ["Plugins.Payments.CustomQuota.Fields.OrderStatusId"] = "Order Status",
                ["Plugins.Payments.CustomQuota.Fields.OrderStatusId.Hint"] = "Order statu default",
                ["Plugins.Payments.CustomQuota.Fields.FrecuencyId"] = "Order Status",
                ["Plugins.Payments.CustomQuota.Fields.FrecuencyId.Hint"] = "Order statu default",
                ["Plugins.Payments.CustomQuota.Fields.StartTimeOn"] = "Start Time",
                ["Plugins.Payments.CustomQuota.Fields.StartTimeOn.Hint"] = "Date Time to begin",

                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Daily}"] = "Daily",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Weekly}"] = "Weekly",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Fortnightly}"] = "Fortnightly",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Monthly}"] = "Monthly",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Bimonthly}"] = "Bimonthly",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Quarterly}"] = "Quarterly",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Semestral}"] = "Semestral",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Annual}"] = "Annual",

                ["Plugins.Payments.CustomQuota.Configuration.Credentials"] = "Settings",

                ["Plugins.Payments.CustomQuota.Quota.List"] = "List of Quota",
                ["Plugins.Payments.CustomQuota.Quota.New"] = "New Quota",
                ["Plugins.Payments.CustomQuota.Quota.Edit"] = "Edit Quota",
                ["Plugins.Payments.CustomQuota.Quota.Create"] = "Create Quota",
                ["Plugins.Payments.CustomQuota.Quota.BackToList"] = "Back To List",

                ["Plugins.Payments.CustomQuota.SearchModel.OrderId"] = "Order Id",
                ["Plugins.Payments.CustomQuota.SearchModel.OrderId.Hint"] = "Order Id",
                ["Plugins.Payments.CustomQuota.SearchModel.OrderStatusId"] = "Order Status",
                ["Plugins.Payments.CustomQuota.SearchModel.OrderStatusId.Hint"] = "Order Status Id"
            };
            var es = new Dictionary<string, string>
            {
                ["Plugins.Payments.CustomQuota.Fields.OrderId"] = "Id Order",
                ["Plugins.Payments.CustomQuota.Fields.OrderId.Hint"] = "Id Order",
                ["Plugins.Payments.CustomQuota.Fields.CustomerId"] = "Id Cliente",
                ["Plugins.Payments.CustomQuota.Fields.CustomerId.Hint"] = "Id Cliente",
                ["Plugins.Payments.CustomQuota.Fields.Quotas"] = "# Cuotas",
                ["Plugins.Payments.CustomQuota.Fields.Quotas.Hint"] = "# Cuotas",
                ["Plugins.Payments.CustomQuota.Fields.OrderNoteId"] = "Id Order Note",
                ["Plugins.Payments.CustomQuota.Fields.OrderNoteId.Hint"] = "Id Order Note",
                ["Plugins.Payments.CustomQuota.Fields.Sequence"] = "Secuencia",
                ["Plugins.Payments.CustomQuota.Fields.Sequence.Hint"] = "Secuencia",
                ["Plugins.Payments.CustomQuota.Fields.Amount"] = "Monto",
                ["Plugins.Payments.CustomQuota.Fields.Amount.Hint"] = "Monto",
                ["Plugins.Payments.CustomQuota.Fields.OrderTotal"] = "Monto Total",
                ["Plugins.Payments.CustomQuota.Fields.OrderTotal.Hint"] = "Monto  Total",
                ["Plugins.Payments.CustomQuota.Fields.PaymentStatusId"] = "Estado de pago",
                ["Plugins.Payments.CustomQuota.Fields.PaymentStatusId.Hint"] = "Estado de pago",
                ["Plugins.Payments.CustomQuota.Fields.SchedulerDateOn"] = "Fecha Programada",
                ["Plugins.Payments.CustomQuota.Fields.SchedulerDateOn.Hint"] = "Fecha Programada",
                ["Plugins.Payments.CustomQuota.Fields.PaymentDateOn"] = "Fecha de pago",
                ["Plugins.Payments.CustomQuota.Fields.PaymentDateOn.Hint"] = "Fecha de pago",
                ["Plugins.Payments.CustomQuota.Fields.CreatedOn"] = "Creado en",
                ["Plugins.Payments.CustomQuota.Fields.CreatedOn.Hint"] = "Fecha de creacion",
                ["Plugins.Payments.CustomQuota.Fields.UpdatedOn"] = "Actualizado en",
                ["Plugins.Payments.CustomQuota.Fields.UpdatedOn.Hint"] = "Fecha de ultima actualizacion",

                ["Plugins.Payments.CustomQuota.Menu.Title"] = "Crear Cuotas",
                ["Plugins.Payments.CustomQuota.Error.NoOrder"] = "El codigo de la orden no existe, ingrese nuevamente el codigo correcto",
                ["Plugins.Payments.CustomQuota.Error.NoQuotaDetailOrder"] = "No hay un información",
                ["Plugins.Payments.CustomQuota.Error.AsPayed"] = "No se puede editar ya que está pagado",
                ["Plugins.Payments.CustomQuota.Error.QuotaExist"] = "No se puede partir en cuotas porque esta orden ya posee coutas",
                ["Plugins.Payments.CustomQuota.Error.QuotasMoreThanOne"] = "El número de cuotas debe ser mayor a 1",

                ["Plugins.Payments.CustomQuota.Deleted"] = "Quotas borradas",
                ["Plugins.Payments.CustomQuota.Order.Note"] = "# Cuota: {0} Fecha Programada: {1} Monto a pagar: {2} Estado: {3}",

                ["Plugins.Payments.CustomQuota.Fields.OrderStatusId"] = "Estado Orden",
                ["Plugins.Payments.CustomQuota.Fields.OrderStatusId.Hint"] = "Estado de la orden por defecto",
                ["Plugins.Payments.CustomQuota.Fields.FrecuencyId"] = "Frecuencia",
                ["Plugins.Payments.CustomQuota.Fields.FrecuencyId.Hint"] = "Frecuencia para crear cuotas",
                ["Plugins.Payments.CustomQuota.Fields.StartTimeOn"] = "Tiempo de inicio",
                ["Plugins.Payments.CustomQuota.Fields.StartTimeOn.Hint"] = "Fecha desde donde inicia",

                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Daily}"] = "Último Dia",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Daily}"] = "Diario",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Weekly}"] = "Semanal",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Fortnightly}"] = "Quincenal",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Monthly}"] = "Mensual",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Bimonthly}"] = "Bimestral",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Quarterly}"] = "Trimestral",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Semestral}"] = "Semestral",
                [$"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}.{FrecuencyList.Annual}"] = "Anual",

                ["Plugins.Payments.CustomQuota.Configuration.Credentials"] = "Configuración",
                ["Plugins.Payments.CustomQuota.Quota.List"] = "Lista de cuotas",
                ["Plugins.Payments.CustomQuota.Quota.New"] = "Nueva cuota",
                ["Plugins.Payments.CustomQuota.Quota.Edit"] = "Editar cuotas",
                ["Plugins.Payments.CustomQuota.Quota.Create"] = "Crear cuotas",
                ["Plugins.Payments.CustomQuota.Quota.BackToList"] = "Volver a la lista",


                ["Plugins.Payments.CustomQuota.SearchModel.OrderId"] = "Id Orden",
                ["Plugins.Payments.CustomQuota.SearchModel.OrderId.Hint"] = "Id Orden",
                ["Plugins.Payments.CustomQuota.SearchModel.OrderStatusId"] = "Estado orden",
                ["Plugins.Payments.CustomQuota.SearchModel.OrderStatusId.Hint"] = "Estado de la orden orden"
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
            await _settingService.DeleteSettingAsync<CustomQuotaSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.CustomQuota");
            await _localizationService.DeleteLocaleResourcesAsync($"{NopLocalizationDefaults.EnumLocaleStringResourcesPrefix}{typeof(FrecuencyList)}");

            //schedule task
            //var task = await _scheduleTaskService.GetTaskByTypeAsync(typeof(CreateCustomQuotaTask).FullName);
            //if (task is not null)
            //    await _scheduleTaskService.DeleteTaskAsync(task);

            await base.UninstallAsync();
        }

        #endregion
    }
}
