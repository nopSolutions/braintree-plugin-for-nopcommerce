using Nop.Services.Events;
using Nop.Services.Payments;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.UI;
using System.Linq;

namespace Nop.Plugin.Payments.BrainTree.Services
{
    /// <summary>
    /// Represents event consumer of the Braintree payment plugin
    /// </summary>
    public class EventConsumer : IConsumer<PageRenderingEvent>
    {
        #region Fields

        //private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        //private readonly IScheduleTaskService _scheduleTaskService;

        #endregion

        #region Ctor

        public EventConsumer(//ILocalizationService localizationService,
            IPaymentService paymentService)
        {
            //this._localizationService = localizationService;
            this._paymentService = paymentService;
            //this._scheduleTaskService = scheduleTaskService;
        }

        #endregion

        /// <summary>
        /// Handle page rendering event
        /// </summary>
        /// <param name="eventMessage">Event message</param>
        public void HandleEvent(PageRenderingEvent eventMessage)
        {
            if (eventMessage?.Helper?.ViewContext?.ActionDescriptor == null)
                return;

            //check whether the plugin is installed and is active
            var squarePaymentMethod = _paymentService.LoadPaymentMethodBySystemName("Payments.BrainTree");
            if (!(squarePaymentMethod?.PluginDescriptor?.Installed ?? false) || !_paymentService.IsPaymentMethodActive(squarePaymentMethod))
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
    }
}
