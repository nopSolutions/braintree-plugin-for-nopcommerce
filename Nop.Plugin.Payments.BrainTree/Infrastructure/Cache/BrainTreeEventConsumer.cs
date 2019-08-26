using System.Linq;
using Nop.Core.Domain.Directory;
using Nop.Core.Events;
using Nop.Plugin.Payments.BrainTree.Services;
using Nop.Services.Events;
using Nop.Services.Payments;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.UI;

namespace Nop.Plugin.Payments.BrainTree.Infrastructure.Cache
{
    /// <summary>
    /// Event consumer of the "Fixed or by weight" shipping plugin (used for removing unused settings)
    /// </summary>
    public partial class BrainTreeEventConsumer : IConsumer<EntityDeletedEvent<Currency>>, IConsumer<PageRenderingEvent>
    {
        #region Fields

        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;
        private readonly IBrainTreeService _brainTreeService;
        private readonly IPaymentPluginManager _paymentPluginManager;

        #endregion

        #region Ctor

        public BrainTreeEventConsumer(BrainTreePaymentSettings brainTreePaymentSettings,
        IBrainTreeService brainTreeService,
            IPaymentPluginManager paymentPluginManager)
        {
            _brainTreePaymentSettings = brainTreePaymentSettings;
            _brainTreeService = brainTreeService;
            _paymentPluginManager = paymentPluginManager;
        }

        #endregion

        #region Methods

        public void HandleEvent(EntityDeletedEvent<Currency> eventMessage)
        {
            _brainTreeService.DeleteMerchants(eventMessage.Entity.CurrencyCode);
        }

        public void HandleEvent(PageRenderingEvent eventMessage)
        {
            if(!_brainTreePaymentSettings.Use3DS)
                return;

            if (eventMessage?.Helper?.ViewContext?.ActionDescriptor == null)
                return;

            //check whether the plugin is installed and is active
            var squarePaymentMethod = _paymentPluginManager.LoadPluginBySystemName(BraintreePaymentDefaults.SystemName);
            if (!(squarePaymentMethod?.PluginDescriptor?.Installed ?? false) || !_paymentPluginManager.IsPluginActive(squarePaymentMethod))
                return;

            //add js script to one page checkout
            if (eventMessage.GetRouteNames().Any(r => r.Equals("CheckoutOnePage")))
            {
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.BraintreeScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.BraintreeClientScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.BraintreeHostedFieldsScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, BraintreePaymentDefaults.Braintree3DSecureScriptPath, excludeFromBundle: true);
            }
        }

        #endregion
    }
}