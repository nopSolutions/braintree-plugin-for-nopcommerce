using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.Braintree.Models;
using Nop.Plugin.Payments.Braintree.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Braintree.Controllers
{
    [Area(AreaNames.Admin)]
    [HttpsRequirement]
    [AutoValidateAntiforgeryToken]
    [ValidateIpAddress]
    [AuthorizeAdmin]
    [ValidateVendor]
    public class BraintreeController : BasePaymentController
    {
        #region Fields

        private readonly BraintreeMerchantService _braintreeMerchantService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public BraintreeController(BraintreeMerchantService braintreeMerchantService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext)
        {
            _braintreeMerchantService = braintreeMerchantService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var braintreePaymentSettings = _settingService.LoadSetting<BraintreePaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                ActiveStoreScopeConfiguration = storeScope,
                UseSandbox = braintreePaymentSettings.UseSandbox,
                PublicKey = braintreePaymentSettings.PublicKey,
                PrivateKey = braintreePaymentSettings.PrivateKey,
                MerchantId = braintreePaymentSettings.MerchantId,
                AdditionalFee = braintreePaymentSettings.AdditionalFee,
                AdditionalFeePercentage = braintreePaymentSettings.AdditionalFeePercentage,
                UseMultiCurrency = braintreePaymentSettings.UseMultiCurrency,
                Use3DS = braintreePaymentSettings.Use3DS
            };

            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(braintreePaymentSettings, settings => settings.UseSandbox, storeScope);
                model.PublicKey_OverrideForStore = _settingService.SettingExists(braintreePaymentSettings, settings => settings.PublicKey, storeScope);
                model.PrivateKey_OverrideForStore = _settingService.SettingExists(braintreePaymentSettings, settings => settings.PrivateKey, storeScope);
                model.MerchantId_OverrideForStore = _settingService.SettingExists(braintreePaymentSettings, settings => settings.MerchantId, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(braintreePaymentSettings, settings => settings.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(braintreePaymentSettings, settings => settings.AdditionalFeePercentage, storeScope);
                model.UseMultiCurrency_OverrideForStore = _settingService.SettingExists(braintreePaymentSettings, settings => settings.UseMultiCurrency, storeScope);
                model.Use3DS_OverrideForStore = _settingService.SettingExists(braintreePaymentSettings, settings => settings.Use3DS, storeScope);
            }

            return View("~/Plugins/Payments.Braintree/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var braintreePaymentSettings = _settingService.LoadSetting<BraintreePaymentSettings>(storeScope);

            //save settings
            braintreePaymentSettings.UseSandbox = model.UseSandbox;
            braintreePaymentSettings.PublicKey = model.PublicKey;
            braintreePaymentSettings.PrivateKey = model.PrivateKey;
            braintreePaymentSettings.MerchantId = model.MerchantId;
            braintreePaymentSettings.AdditionalFee = model.AdditionalFee;
            braintreePaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            braintreePaymentSettings.UseMultiCurrency = model.UseMultiCurrency;
            braintreePaymentSettings.Use3DS = model.Use3DS;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(braintreePaymentSettings, settings => settings.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(braintreePaymentSettings, settings => settings.PublicKey, model.PublicKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(braintreePaymentSettings, settings => settings.PrivateKey, model.PrivateKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(braintreePaymentSettings, settings => settings.MerchantId, model.MerchantId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(braintreePaymentSettings, settings => settings.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(braintreePaymentSettings, settings => settings.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(braintreePaymentSettings, settings => settings.UseMultiCurrency, model.UseMultiCurrency_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(braintreePaymentSettings, settings => settings.Use3DS, model.Use3DS_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [HttpPost]
        public virtual IActionResult GetCurrencies(ConfigurationModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedDataTablesJson();

            //load settings for a chosen store scope
            var storeId = _storeContext.ActiveStoreScopeConfiguration;

            //get merchant records
            var merchantRecords = _braintreeMerchantService.GetMerchants(storeId);

            //prepare model
            var model = new CurrencyListModel().PrepareToGrid(searchModel, merchantRecords, () =>
            {
                return merchantRecords.Select(currency => new CurrencyModel
                {
                    Id = currency.Id,
                    CurrencyCode = currency.CurrencyCode,
                    MerchantAccountId = currency.MerchantAccountId
                });
            });

            return Json(model);
        }

        [HttpPost]
        public virtual IActionResult UpdateCurrency(CurrencyModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            _braintreeMerchantService.UpdateMerchant(model.Id, model.MerchantAccountId);

            return new NullJsonResult();
        }

        #endregion
    }
}