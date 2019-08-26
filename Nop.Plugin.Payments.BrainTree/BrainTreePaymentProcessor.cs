using System;
using System.Collections.Generic;
using System.Linq;
using Braintree;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.BrainTree.Controllers;
using Nop.Plugin.Payments.BrainTree.Data;
using Nop.Plugin.Payments.BrainTree.Models;
using Nop.Plugin.Payments.BrainTree.Services;
using Nop.Plugin.Payments.BrainTree.Validators;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Environment = Braintree.Environment;

namespace Nop.Plugin.Payments.BrainTree
{
    public class BrainTreePaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Constants

        /// <summary>
        /// nopCommerce partner code
        /// </summary>
        private const string BN_CODE = "nopCommerceCart";

        #endregion

        #region Fields

        private readonly BrainTreeObjectContext _objectContext;
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;
        private readonly IBrainTreeService _brainTreeService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public BrainTreePaymentProcessor(BrainTreeObjectContext objectContext,
            BrainTreePaymentSettings brainTreePaymentSettings,
            IBrainTreeService brainTreeService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            ILocalizationService localizationService,
            IPaymentService paymentService,
            ISettingService settingService,
            IWebHelper webHelper,
            IWorkContext workContext)
        {
            _objectContext = objectContext;
            _brainTreePaymentSettings = brainTreePaymentSettings;
            _brainTreeService = brainTreeService;
            _currencyService = currencyService;
            _customerService = customerService;
            _localizationService = localizationService;
            _paymentService = paymentService;
            _settingService = settingService;
            _webHelper = webHelper;
            _workContext = workContext;
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

            //search to see if customer is already in the vault
            var searchRequest = new CustomerSearchRequest().Email.Is(customer.BillingAddress.Email);
            var vaultCustomer = gateway.Customer.Search(searchRequest);

            var customerId = vaultCustomer.FirstOrDefault()?.Id ?? string.Empty;

            var currencyMerchantId = string.Empty;
            var amount = processPaymentRequest.OrderTotal;

            if (_brainTreePaymentSettings.UseMultiCurrency)
            {
                //get currency
                var currency = _workContext.WorkingCurrency;

                currencyMerchantId = _brainTreeService.GetMerchantId(currency.CurrencyCode);

                if (!string.IsNullOrEmpty(currencyMerchantId))
                    amount = _currencyService.ConvertCurrency(amount, currency.Rate);
            }

            //new transaction request
            var transactionRequest = new TransactionRequest
            {
                Amount = amount,
                CustomerId = customerId,
                Channel = BN_CODE,
                MerchantAccountId = currencyMerchantId
            };

            if (_brainTreePaymentSettings.Use3DS)
            {
                transactionRequest.PaymentMethodNonce = processPaymentRequest.CustomValues["CardNonce"].ToString();
            }
            else
            {
                //transaction credit card request
                var transactionCreditCardRequest = new TransactionCreditCardRequest
                {
                    Number = processPaymentRequest.CreditCardNumber,
                    CVV = processPaymentRequest.CreditCardCvv2,
                    ExpirationDate = processPaymentRequest.CreditCardExpireMonth + "/" + processPaymentRequest.CreditCardExpireYear,
                };
                transactionRequest.CreditCard = transactionCreditCardRequest;
            }

            //customer request
            var customerRequest = new CustomerRequest
            {
                CustomerId = customerId,
                FirstName = customer.BillingAddress.FirstName,
                LastName = customer.BillingAddress.LastName,
                Email = customer.BillingAddress.Email,
                Fax = customer.BillingAddress.FaxNumber,
                Company = customer.BillingAddress.Company,
                Phone = customer.BillingAddress.PhoneNumber
            };
            transactionRequest.Customer = customerRequest;

            //address request
            var addressRequest = new AddressRequest
            {
                FirstName = customer.BillingAddress.FirstName,
                LastName = customer.BillingAddress.LastName,
                StreetAddress = customer.BillingAddress.Address1,
                PostalCode = customer.BillingAddress.ZipPostalCode,
                CountryCodeAlpha2 = customer.BillingAddress.Country?.TwoLetterIsoCode,
                CountryCodeAlpha3 = customer.BillingAddress.Country?.ThreeLetterIsoCode
            };
            transactionRequest.BillingAddress = addressRequest;
            
            //transaction options request
            var transactionOptionsRequest = new TransactionOptionsRequest
            {
                SubmitForSettlement = true,
                ThreeDSecure = new TransactionOptionsThreeDSecureRequest()
            };
            transactionRequest.Options = transactionOptionsRequest;

            //sending a request
            var result = gateway.Transaction.Sale(transactionRequest);

            //result
            if (result.IsSuccess())
                processPaymentResult.NewPaymentStatus = PaymentStatus.Paid;
            else
                processPaymentResult.AddError("Error processing payment." + result.Message);

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

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentBrainTree/Configure";
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_brainTreePaymentSettings, _localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"],
                CardNonce = form["CardNonce"]
            };

            var validationResult = validator.Validate(model);

            if (validationResult.IsValid)
                return warnings;

            warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return warnings;
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = _brainTreePaymentSettings.Use3DS ? new ProcessPaymentRequest() : new ProcessPaymentRequest
            {
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"]
            };

            if (form.TryGetValue("CardNonce", out var cardNonce) && !StringValues.IsNullOrEmpty(cardNonce))
                paymentInfo.CustomValues.Add("CardNonce", cardNonce.ToString());

            return paymentInfo;
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
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
                MerchantId = string.Empty,
                PrivateKey = string.Empty,
                PublicKey = string.Empty
            };
            _settingService.SaveSetting(settings);

            //database objects
            _objectContext.Install();
            
            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Currency.Fields.CurrencyCode", "Currency code");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Currency.Fields.MerchantAccountId", "Merchant account");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee", "Additional fee");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId.Hint", "Enter Merchant ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId", "Merchant ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey.Hint", "Enter Private key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey", "Private Key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey.Hint", "Enter Public key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey", "Public Key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.Use3DS", "Use the 3D secure");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.Use3DS.Hint", "Check to enable the 3D secure integration");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseMultiCurrency.Hint", "Check to enable multi currency support (MerchantAccount)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseMultiCurrency", "Use multi currency");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox", "Use Sandbox");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.PaymentMethodDescription", "Pay by credit / debit card");

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<BrainTreePaymentSettings>();

            //database objects
            _objectContext.Uninstall();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Currency.Fields.CurrencyCode");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Currency.Fields.MerchantAccountId");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseMultiCurrency.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseMultiCurrency");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.BrainTree.PaymentMethodDescription");

            base.Uninstall();
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
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription => _localizationService.GetResource("Plugins.Payments.BrainTree.PaymentMethodDescription");

        #endregion
    }
}
