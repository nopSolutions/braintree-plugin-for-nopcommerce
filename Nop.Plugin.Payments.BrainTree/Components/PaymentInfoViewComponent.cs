using System;
using System.Threading.Tasks;
using Braintree;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Braintree.Models;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Web.Framework.Components;
using Environment = Braintree.Environment;

namespace Nop.Plugin.Payments.Braintree.Components
{
    /// <summary>
    /// Represents the view component to display payment info in public store
    /// </summary>
    [ViewComponent(Name = "BraintreePaymentInfo")]
    public class PaymentInfoViewComponent : NopViewComponent
    {
        #region Fields

        private readonly BraintreePaymentSettings _braintreePaymentSettings;
        private readonly INotificationService _notificationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly OrderSettings _orderSettings;

        #endregion

        #region Ctor

        public PaymentInfoViewComponent(BraintreePaymentSettings braintreePaymentSettings,
            INotificationService notificationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IWorkContext workContext,
            OrderSettings orderSettings)
        {
            _braintreePaymentSettings = braintreePaymentSettings;
            _notificationService = notificationService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _workContext = workContext;
            _orderSettings = orderSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invoke view component
        /// </summary>
        /// <param name="widgetZone">Widget zone name</param>
        /// <param name="additionalData">Additional data</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the view component result
        /// </returns>
        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            var model = new PaymentInfoModel();

            if (_braintreePaymentSettings.Use3DS)
            {
                try
                {
                    var gateway = new BraintreeGateway
                    {
                        Environment = _braintreePaymentSettings.UseSandbox ? Environment.SANDBOX : Environment.PRODUCTION,
                        MerchantId = _braintreePaymentSettings.MerchantId,
                        PublicKey = _braintreePaymentSettings.PublicKey,
                        PrivateKey = _braintreePaymentSettings.PrivateKey
                    };
                    var clientToken = gateway.ClientToken.Generate();

                    var cart = await _shoppingCartService
                        .GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);
                    var (orderTotal, _, _, _, _, _) = await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart);

                    model.ClientToken = clientToken;
                    model.OrderTotal = orderTotal;
                }
                catch (Exception exception)
                {
                    model.Errors = exception.Message;
                    if (_orderSettings.OnePageCheckoutEnabled)
                        ModelState.AddModelError(string.Empty, exception.Message);
                    else
                        await _notificationService.ErrorNotificationAsync(exception);
                }

                return View("~/Plugins/Payments.Braintree/Views/PaymentInfo.3DS.cshtml", model);
            }

            for (var i = 0; i < 15; i++)
            {
                var year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem { Text = year, Value = year, });
            }

            for (var i = 1; i <= 12; i++)
            {
                var text = (i < 10) ? "0" + i : i.ToString();
                model.ExpireMonths.Add(new SelectListItem { Text = text, Value = i.ToString(), });
            }

            return View("~/Plugins/Payments.Braintree/Views/PaymentInfo.cshtml", model);
        }

        #endregion
    }
}