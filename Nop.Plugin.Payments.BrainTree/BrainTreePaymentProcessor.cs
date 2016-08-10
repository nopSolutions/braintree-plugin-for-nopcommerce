using System;
using System.Collections.Generic;
using System.Web.Routing;
using Braintree;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.BrainTree.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Environment = Braintree.Environment;
using Nop.Services.Localization;

namespace Nop.Plugin.Payments.BrainTree
{
    public class BrainTreePaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly ISettingService _settingService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;

        #endregion

        #region Ctor

        public BrainTreePaymentProcessor(ICustomerService customerService, 
            ISettingService settingService, 
            IOrderTotalCalculationService orderTotalCalculationService,
            BrainTreePaymentSettings brainTreePaymentSettings)
        {
            this._customerService = customerService;
            this._settingService = settingService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._brainTreePaymentSettings = brainTreePaymentSettings;
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
            bool useSandBox = _brainTreePaymentSettings.UseSandBox;
            string merchantId = _brainTreePaymentSettings.MerchantId;
            string publicKey = _brainTreePaymentSettings.PublicKey;
            string privateKey = _brainTreePaymentSettings.PrivateKey;

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
                Amount = processPaymentRequest.OrderTotal
            };

            //transaction credit card request
            var transactionCreditCardRequest = new TransactionCreditCardRequest
            {
                Number = processPaymentRequest.CreditCardNumber,
                CVV = processPaymentRequest.CreditCardCvv2,
                ExpirationDate = processPaymentRequest.CreditCardExpireMonth + "/" + processPaymentRequest.CreditCardExpireYear
            };
            transactionRequest.CreditCard = transactionCreditCardRequest;

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
            Result<Transaction> result = gateway.Transaction.Sale(transactionRequest);

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
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
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
                throw new ArgumentNullException("order");

            //it's not a redirection payment method. So we always return false
            return false;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentBrainTree";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.BrainTree.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentBrainTree";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.BrainTree.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentBrainTreeController);
        }

        public override void Install()
        {
            //settings
            var settings = new BrainTreePaymentSettings()
            {
                UseSandBox = true,
                MerchantId = "",
                PrivateKey = "",
                PublicKey = ""
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox", "Use Sandbox");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");

            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId", "Merchant ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId.Hint", "Enter Merchant ID");

            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey", "Public Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey.Hint", "Enter Public key");

            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey", "Private Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey.Hint", "Enter Private key");

            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            
            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<BrainTreePaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.UseSandbox.Hint");

            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.MerchantId.Hint");

            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PublicKey.Hint");

            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.PrivateKey.Hint");

            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint");

            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint");

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
                return RecurringPaymentType.Manual;
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

        #endregion

    }
}
