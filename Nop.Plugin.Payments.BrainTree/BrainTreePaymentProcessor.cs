using System;
using System.Collections.Generic;
using Braintree;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.BrainTree.Controllers;
using Nop.Plugin.Payments.BrainTree.Models;
using Nop.Plugin.Payments.BrainTree.Validators;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Environment = Braintree.Environment;

namespace Nop.Plugin.Payments.BrainTree
{
    public class BrainTreePaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Constants

        /// <summary>
        /// nopCommerce partner code
        /// </summary>
        private const string BN_CODE = "nopCommerce_SP";

        #endregion

        #region Fields

        private readonly ICustomerService _customerService;
        private readonly ISettingService _settingService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPaymentService _paymentService;
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public BrainTreePaymentProcessor(ICustomerService customerService,
            ISettingService settingService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPaymentService paymentService,
            BrainTreePaymentSettings brainTreePaymentSettings,
            ILocalizationService localizationService,
            IWebHelper webHelper)
        {
            this._customerService = customerService;
            this._settingService = settingService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._paymentService = paymentService;
            this._brainTreePaymentSettings = brainTreePaymentSettings;
            this._localizationService = localizationService;
            this._webHelper = webHelper;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var processPaymentResult = new ProcessPaymentResult();

            //get customer
            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);

            //get settings
            var useSandBox = _brainTreePaymentSettings.UseSandBox;
            var merchantId = _brainTreePaymentSettings.MerchantId;
            var publicKey = _brainTreePaymentSettings.PublicKey;
            var privateKey = _brainTreePaymentSettings.PrivateKey;

            //new gateway
            var gateway = new BraintreeGateway
            {
                Environment = useSandBox ? Environment.SANDBOX : Environment.PRODUCTION,
                MerchantId = merchantId,
                PublicKey = publicKey,
                PrivateKey = privateKey
            };

            //new transaction request
            var transactionRequest = new TransactionRequest
            {
                Amount = processPaymentRequest.OrderTotal,
                Channel = BN_CODE,
                PaymentMethodNonce = processPaymentRequest.CustomValues["CardNonce"].ToString(),
            };

            //transaction credit card request
            //var transactionCreditCardRequest = new TransactionCreditCardRequest
            //{
            //    Number = processPaymentRequest.CreditCardNumber,
            //    CVV = processPaymentRequest.CreditCardCvv2,
            //    ExpirationDate = processPaymentRequest.CreditCardExpireMonth + "/" + processPaymentRequest.CreditCardExpireYear
            //};
            //transactionRequest.CreditCard = transactionCreditCardRequest;

            //address request
            var addressRequest = new AddressRequest
            {
                FirstName = customer.BillingAddress.FirstName,
                LastName = customer.BillingAddress.LastName,
                StreetAddress = customer.BillingAddress.Address1,
                PostalCode = customer.BillingAddress.ZipPostalCode
            };
            transactionRequest.BillingAddress = addressRequest;

            //transaction options request
            var transactionOptionsRequest = new TransactionOptionsRequest
            {
                SubmitForSettlement = true
            };
            transactionRequest.Options = transactionOptionsRequest;

            //sending a request
            var result = gateway.Transaction.Sale(transactionRequest);

            //result
            if (result.IsSuccess())
            {
                processPaymentResult.NewPaymentStatus = PaymentStatus.Paid;
            }
            else
            {
                processPaymentResult.AddError("Error processing payment." + result.Message);
            }

            return processPaymentResult;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //nothing
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = _paymentService.CalculateAdditionalFee(cart,
                _brainTreePaymentSettings.AdditionalFee, _brainTreePaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //it's not a redirection payment method. So we always return false
            return false;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentBrainTree/Configure";
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                //CardholderName = form["CardholderName"],
                //CardNumber = form["CardNumber"],
                //CardCode = form["CardCode"],
                //ExpireMonth = form["ExpireMonth"],
                //ExpireYear = form["ExpireYear"]
                CardNonce = form["CardNonce"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                {
                    warnings.Add(error.ErrorMessage);
                }
            return warnings;
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest
            {
                //CreditCardName = form["CardholderName"],
                //CreditCardNumber = form["CardNumber"],
                //CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                //CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                //CreditCardCvv2 = form["CardCode"]
                
            };
            if (form.TryGetValue("CardNonce", out StringValues cardNonce) && !StringValues.IsNullOrEmpty(cardNonce))
                paymentInfo.CustomValues.Add("CardNonce", cardNonce.ToString());
            
            return paymentInfo;
        }

        public string GetPublicViewComponentName()
        {
            return "PaymentBrainTree";
        }

        public Type GetControllerType()
        {
            return typeof(PaymentBrainTreeController);
        }

        public override void Install()
        {
            //settings
            var settings = new BrainTreePaymentSettings
            {
                UseSandBox = true,
                MerchantId = "",
                PrivateKey = "",
                PublicKey = ""
            };
            _settingService.SaveSetting(settings);

            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox", "Use Sandbox");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId", "Merchant ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId.Hint", "Enter Merchant ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey", "Public Key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey.Hint", "Enter Public key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey", "Private Key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey.Hint", "Enter Private key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee", "Additional fee");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.PaymentMethodDescription", "Pay by credit / debit card");

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<BrainTreePaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.PaymentMethodDescription");

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Standard;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            get { return _localizationService.GetResource("Plugins.Payments.BrainTree.PaymentMethodDescription"); }
        }

        #endregion

    }
}
