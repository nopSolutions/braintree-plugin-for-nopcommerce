using System.Threading.Tasks;
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
        private readonly INopHtmlHelper _nopHtmlHelper;
        private readonly IPaymentPluginManager _paymentPluginManager;

        #endregion

        #region Ctor

        public EventConsumer(BraintreeMerchantService braintreeMerchantService,
            BraintreePaymentSettings braintreePaymentSettings,
            INopHtmlHelper nopHtmlHelper,
            IPaymentPluginManager paymentPluginManager)
        {
            _braintreeMerchantService = braintreeMerchantService;
            _braintreePaymentSettings = braintreePaymentSettings;
            _nopHtmlHelper = nopHtmlHelper;
            _paymentPluginManager = paymentPluginManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handle event
        /// </summary>
        /// <param name="eventMessage">Event</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleEventAsync(EntityDeletedEvent<Currency> eventMessage)
        {
            await _braintreeMerchantService.DeleteMerchantsAsync(eventMessage.Entity.CurrencyCode);
        }

        /// <summary>
        /// Handle event
        /// </summary>
        /// <param name="eventMessage">Event</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleEventAsync(PageRenderingEvent eventMessage)
        {
            if (!await _paymentPluginManager.IsPluginActiveAsync(BraintreePaymentDefaults.SystemName))
                return;

            if (!_braintreePaymentSettings.Use3DS)
                return;
            
            //add js script to one page checkout
            var routeName = eventMessage.GetRouteName() ?? string.Empty;
            if (routeName == BraintreePaymentDefaults.OnePageCheckoutRouteName)
            {
                _nopHtmlHelper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.ScriptPath, excludeFromBundle: true);
                _nopHtmlHelper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.ClientScriptPath, excludeFromBundle: true);
                _nopHtmlHelper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.HostedFieldsScriptPath, excludeFromBundle: true);
                _nopHtmlHelper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.SecureScriptPath, excludeFromBundle: true);
            }
        }

        #endregion
    }
}