using Nop.Core.Domain.Directory;
using Nop.Core.Events;
using Nop.Services.Events;
using Nop.Services.Payments;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.UI;

namespace Nop.Plugin.Payments.Braintree.Services
{
    /// <summary>
    /// Represents plugin event consumer
    /// </summary>
    public class EventConsumer :
        IConsumer<EntityDeletedEvent<Currency>>,
        IConsumer<PageRenderingEvent>
    {
        #region Fields

        private readonly BraintreeMerchantService _braintreeMerchantService;
        private readonly BraintreePaymentSettings _braintreePaymentSettings;
        private readonly IPaymentPluginManager _paymentPluginManager;

        #endregion

        #region Ctor

        public EventConsumer(BraintreeMerchantService braintreeMerchantService,
            BraintreePaymentSettings braintreePaymentSettings,
            IPaymentPluginManager paymentPluginManager)
        {
            _braintreeMerchantService = braintreeMerchantService;
            _braintreePaymentSettings = braintreePaymentSettings;
            _paymentPluginManager = paymentPluginManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handle currency deleted event
        /// </summary>
        /// <param name="eventMessage">Event message</param>
        public void HandleEvent(EntityDeletedEvent<Currency> eventMessage)
        {
            _braintreeMerchantService.DeleteMerchants(eventMessage.Entity.CurrencyCode);
        }

        /// <summary>
        /// Handle page rendering event
        /// </summary>
        /// <param name="eventMessage">Event message</param>
        public void HandleEvent(PageRenderingEvent eventMessage)
        {
            if (!_paymentPluginManager.IsPluginActive(BraintreePaymentDefaults.SystemName))
                return;

            if (!_braintreePaymentSettings.Use3DS)
                return;

            if (eventMessage?.Helper?.ViewContext?.ActionDescriptor == null)
                return;

            //add js script to one page checkout
            var routeName = eventMessage.GetRouteName() ?? string.Empty;
            if (routeName == BraintreePaymentDefaults.OnePageCheckoutRouteName)
            {
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.ScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.ClientScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.HostedFieldsScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.SecureScriptPath, excludeFromBundle: true);

            }
        }

        #endregion
    }
}