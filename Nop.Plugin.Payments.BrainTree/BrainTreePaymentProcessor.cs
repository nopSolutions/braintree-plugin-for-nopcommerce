using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Braintree;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Braintree.Components;
using Nop.Plugin.Payments.Braintree.Models;
using Nop.Plugin.Payments.Braintree.Services;
using Nop.Plugin.Payments.Braintree.Validators;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Environment = Braintree.Environment;

namespace Nop.Plugin.Payments.Braintree
{
    /// <summary>
    /// Represents a payment method implementation
    /// </summary>
    public class BraintreePaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly BraintreeMerchantService _braintreeMerchantService;
        private readonly BraintreePaymentSettings _braintreePaymentSettings;
        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public BraintreePaymentProcessor(BraintreeMerchantService braintreeMerchantService,
            BraintreePaymentSettings braintreePaymentSettings,
            IAddressService addressService,
            ICountryService countryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ISettingService settingService,
            IWebHelper webHelper,
            IWorkContext workContext)
        {
            _braintreeMerchantService = braintreeMerchantService;
            _braintreePaymentSettings = braintreePaymentSettings;
            _addressService = addressService;
            _countryService = countryService;
            _currencyService = currencyService;
            _customerService = customerService;
            _localizationService = localizationService;
            _orderTotalCalculationService = orderTotalCalculationService;
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
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var processPaymentResult = new ProcessPaymentResult();

            //get customer
            var customer = await _customerService.GetCustomerByIdAsync(processPaymentRequest.CustomerId);

            //get settings
            var useSandbox = _braintreePaymentSettings.UseSandbox;
            var merchantId = _braintreePaymentSettings.MerchantId;
            var publicKey = _braintreePaymentSettings.PublicKey;
            var privateKey = _braintreePaymentSettings.PrivateKey;

            //new gateway
            var gateway = new BraintreeGateway
            {
                Environment = useSandbox ? Environment.SANDBOX : Environment.PRODUCTION,
                MerchantId = merchantId,
                PublicKey = publicKey,
                PrivateKey = privateKey
            };

            var billingAddress = await _addressService.GetAddressByIdAsync(customer.BillingAddressId ?? 0);

            //search to see if customer is already in the vault
            var searchRequest = new CustomerSearchRequest().Email.Is(billingAddress?.Email);
            var vaultCustomer = gateway.Customer.Search(searchRequest);

            var customerId = vaultCustomer.FirstOrDefault()?.Id ?? string.Empty;

            var currencyMerchantId = string.Empty;
            var amount = processPaymentRequest.OrderTotal;

            if (_braintreePaymentSettings.UseMultiCurrency)
            {
                //get currency
                var currency = await _workContext.GetWorkingCurrencyAsync();

                currencyMerchantId = await _braintreeMerchantService.GetMerchantIdAsync(currency.CurrencyCode);

                if (!string.IsNullOrEmpty(currencyMerchantId))
                    amount = _currencyService.ConvertCurrency(amount, currency.Rate);
            }

            //new transaction request
            var transactionRequest = new TransactionRequest
            {
                Amount = amount,
                CustomerId = customerId,
                Channel = BraintreePaymentDefaults.PartnerCode,
                MerchantAccountId = currencyMerchantId
            };

            if (_braintreePaymentSettings.Use3DS)
            {
                if (processPaymentRequest.CustomValues.ContainsKey("CardNonce"))
                    transactionRequest.PaymentMethodNonce = processPaymentRequest.CustomValues["CardNonce"].ToString();
                else
                {
                    processPaymentResult.AddError(await _localizationService.GetResourceAsync("Plugins.Payments.Braintree.Errors.3DSecureFailed"));
                    return processPaymentResult;
                }
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
                FirstName = billingAddress?.FirstName,
                LastName = billingAddress?.LastName,
                Email = billingAddress?.Email,
                Fax = billingAddress?.FaxNumber,
                Company = billingAddress?.Company,
                Phone = billingAddress?.PhoneNumber
            };
            transactionRequest.Customer = customerRequest;

            var country = await _countryService.GetCountryByAddressAsync(billingAddress);

            //address request
            var addressRequest = new AddressRequest
            {
                FirstName = billingAddress?.FirstName,
                LastName = billingAddress?.LastName,
                StreetAddress = billingAddress?.Address1,
                PostalCode = billingAddress?.ZipPostalCode,
                CountryCodeAlpha2 = country?.TwoLetterIsoCode,
                CountryCodeAlpha3 = country?.ThreeLetterIsoCode
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
                processPaymentResult.AddError((await _localizationService.GetResourceAsync("Plugins.Payments.Braintree.Errors.ErrorProcessingPayment")) + result.Message);

            return processPaymentResult;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //nothing
            return Task.CompletedTask;
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
            return Task.FromResult(false);
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
            var result = await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
                _braintreePaymentSettings.AdditionalFee, _braintreePaymentSettings.AdditionalFeePercentage);

            return result;
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
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");

            return Task.FromResult(result);
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
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");

            return Task.FromResult(result);
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
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");

            return Task.FromResult(result);
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
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");

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
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");

            return Task.FromResult(result);
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
            return Task.FromResult(false);
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
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_braintreePaymentSettings, _localizationService);
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
                return Task.FromResult<IList<string>>(warnings);

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
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var paymentInfo = _braintreePaymentSettings.Use3DS ? new ProcessPaymentRequest() : new ProcessPaymentRequest
            {
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"]
            };

            if (form.TryGetValue("CardNonce", out var cardNonce) && !StringValues.IsNullOrEmpty(cardNonce))
                paymentInfo.CustomValues.Add("CardNonce", cardNonce.ToString());

            return Task.FromResult(paymentInfo);
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/Braintree/Configure";
        }

        /// <summary>
        /// Gets a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        public Type GetPublicViewComponent()
        {
            return typeof(PaymentInfoViewComponent);
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            //settings
            await _settingService.SaveSettingAsync(new BraintreePaymentSettings
            {
                UseSandbox = true
            });

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Payments.Braintree.Currency.Fields.CurrencyCode"] = "Currency code",
                ["Plugins.Payments.Braintree.Currency.Fields.MerchantAccountId"] = "Merchant account",
                ["Plugins.Payments.Braintree.Errors.3DSecureFailed"] = "The 3D Secure authentication is failed",
                ["Plugins.Payments.Braintree.Errors.ErrorProcessingPayment"] = "Error processing payment.",
                ["Plugins.Payments.Braintree.Fields.AdditionalFee.Hint"] = "Enter additional fee to charge your customers.",
                ["Plugins.Payments.Braintree.Fields.AdditionalFee"] = "Additional fee",
                ["Plugins.Payments.Braintree.Fields.AdditionalFeePercentage.Hint"] = "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.",
                ["Plugins.Payments.Braintree.Fields.AdditionalFeePercentage"] = "Additional fee. Use percentage",
                ["Plugins.Payments.Braintree.Fields.MerchantId.Hint"] = "Enter Merchant ID",
                ["Plugins.Payments.Braintree.Fields.MerchantId.Required"] = "Merchant ID is required",
                ["Plugins.Payments.Braintree.Fields.MerchantId"] = "Merchant ID",
                ["Plugins.Payments.Braintree.Fields.PrivateKey.Hint"] = "Enter Private key",
                ["Plugins.Payments.Braintree.Fields.PrivateKey.Required"] = "Private key is required",
                ["Plugins.Payments.Braintree.Fields.PrivateKey"] = "Private Key",
                ["Plugins.Payments.Braintree.Fields.PublicKey.Hint"] = "Enter Public key",
                ["Plugins.Payments.Braintree.Fields.PublicKey.Required"] = "Public key is required",
                ["Plugins.Payments.Braintree.Fields.PublicKey"] = "Public Key",
                ["Plugins.Payments.Braintree.Fields.Use3DS"] = "Use the 3D secure",
                ["Plugins.Payments.Braintree.Fields.Use3DS.Hint"] = "Check to enable the 3D secure integration",
                ["Plugins.Payments.Braintree.Fields.UseMultiCurrency.Hint"] = "Check to enable multi currency support (MerchantAccount)",
                ["Plugins.Payments.Braintree.Fields.UseMultiCurrency"] = "Use multi currency",
                ["Plugins.Payments.Braintree.Fields.UseSandbox.Hint"] = "Check to enable Sandbox (testing environment).",
                ["Plugins.Payments.Braintree.Fields.UseSandbox"] = "Use Sandbox",
                ["Plugins.Payments.Braintree.PaymentMethodDescription"] = "Pay by credit / debit card"
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<BraintreePaymentSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.Braintree");

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
            return await _localizationService.GetResourceAsync("Plugins.Payments.Braintree.PaymentMethodDescription");
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

        #endregion
    }
}
