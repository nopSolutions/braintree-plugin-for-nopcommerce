using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.BrainTree.Models;
using Nop.Plugin.Payments.BrainTree.Validators;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.BrainTree.Controllers
{
    public class PaymentBrainTreeController : BasePaymentController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;

        #endregion

        #region Ctor

        public PaymentBrainTreeController(ISettingService settingService,
            ILocalizationService localizationService, 
            IWorkContext workContext, 
            IStoreService storeService)
        {
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._workContext = workContext;
            this._storeService = storeService;
        }

        #endregion

        #region Methods

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var brainTreePaymentSettings = _settingService.LoadSetting<BrainTreePaymentSettings>(storeScope);

            var model = new ConfigurationModel
                            {
                                ActiveStoreScopeConfiguration = storeScope,
                                UseSandBox = brainTreePaymentSettings.UseSandBox,
                                PublicKey = brainTreePaymentSettings.PublicKey,
                                PrivateKey = brainTreePaymentSettings.PrivateKey,
                                MerchantId = brainTreePaymentSettings.MerchantId,
                                AdditionalFee = brainTreePaymentSettings.AdditionalFee,
                                AdditionalFeePercentage = brainTreePaymentSettings.AdditionalFeePercentage
                            };

            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(brainTreePaymentSettings, x => x.UseSandBox, storeScope);
                model.PublicKey_OverrideForStore = _settingService.SettingExists(brainTreePaymentSettings, x => x.PublicKey, storeScope);
                model.PrivateKey_OverrideForStore = _settingService.SettingExists(brainTreePaymentSettings, x => x.PrivateKey, storeScope);
                model.MerchantId_OverrideForStore = _settingService.SettingExists(brainTreePaymentSettings, x => x.MerchantId, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(brainTreePaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(brainTreePaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.BrainTree/Views/PaymentBrainTree/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var brainTreePaymentSettings = _settingService.LoadSetting<BrainTreePaymentSettings>(storeScope);

            //save settings
            brainTreePaymentSettings.UseSandBox = model.UseSandBox;
            brainTreePaymentSettings.PublicKey = model.PublicKey;
            brainTreePaymentSettings.PrivateKey = model.PrivateKey;
            brainTreePaymentSettings.MerchantId = model.MerchantId;
            brainTreePaymentSettings.AdditionalFee = model.AdditionalFee;
            brainTreePaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(brainTreePaymentSettings, x => x.UseSandBox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(brainTreePaymentSettings, x => x.PublicKey, model.PublicKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(brainTreePaymentSettings, x => x.PrivateKey, model.PrivateKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(brainTreePaymentSettings, x => x.MerchantId, model.MerchantId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(brainTreePaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(brainTreePaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            
            //years
            for (int i = 0; i < 15; i++)
            {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem()
                {
                    Text = year,
                    Value = year,
                });
            }

            //months
            for (int i = 1; i <= 12; i++)
            {
                string text = (i < 10) ? "0" + i : i.ToString();
                model.ExpireMonths.Add(new SelectListItem()
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }

            //set postback values
            var form = this.Request.Form;
            model.CardholderName = form["CardholderName"];
            model.CardNumber = form["CardNumber"];
            model.CardCode = form["CardCode"];

            var selectedMonth = model.ExpireMonths.Where(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedMonth != null)
                selectedMonth.Selected = true;
            var selectedYear = model.ExpireYears.Where(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedYear != null)
                selectedYear.Selected = true;

            return View("~/Plugins/Payments.BrainTree/Views/PaymentBrainTree/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel()
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                {
                    warnings.Add(error.ErrorMessage);
                }
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest
                                  {
                                      CreditCardName = form["CardholderName"],
                                      CreditCardNumber = form["CardNumber"],
                                      CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                                      CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                                      CreditCardCvv2 = form["CardCode"]
                                  };
            return paymentInfo;
        }

        #endregion
    }
}