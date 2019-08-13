using System;
using System.Linq;
using System.Net;
using Braintree;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Http.Extensions;
using Nop.Plugin.Payments.BrainTree.Models;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.BrainTree.Components
{
    [ViewComponent(Name = "PaymentBrainTree")]
    public class PaymentBrainTreeViewComponent : NopViewComponent
    {
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;

        public PaymentBrainTreeViewComponent(BrainTreePaymentSettings brainTreePaymentSettings,
            IWorkContext workContext,
            IStoreContext storeContext,
            IOrderTotalCalculationService orderTotalCalculationService)
        {
            _brainTreePaymentSettings = brainTreePaymentSettings;
            _workContext = workContext;
            _storeContext = storeContext;
            _orderTotalCalculationService = orderTotalCalculationService;
        }

        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel();

            //years
            //for (var i = 0; i < 15; i++)
            //{
            //    var year = Convert.ToString(DateTime.Now.Year + i);
            //    model.ExpireYears.Add(new SelectListItem
            //    {
            //        Text = year,
            //        Value = year,
            //    });
            //}

            ////months
            //for (var i = 1; i <= 12; i++)
            //{
            //    var text = (i < 10) ? "0" + i : i.ToString();
            //    model.ExpireMonths.Add(new SelectListItem
            //    {
            //        Text = text,
            //        Value = i.ToString(),
            //    });
            //}

            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                   .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                   .LimitPerStore(_storeContext.CurrentStore.Id)
                   .ToList();

            if (!cart.Any())
                throw new Exception("Your cart is empty");

            //get settings
            var useSandBox = _brainTreePaymentSettings.UseSandBox;
            var merchantId = _brainTreePaymentSettings.MerchantId;
            var publicKey = _brainTreePaymentSettings.PublicKey;
            var privateKey = _brainTreePaymentSettings.PrivateKey;

            var gateway = new BraintreeGateway
            {
                Environment = useSandBox ? Braintree.Environment.SANDBOX : Braintree.Environment.PRODUCTION,
                MerchantId = merchantId,
                PublicKey = publicKey,
                PrivateKey = privateKey
            };

            
            ViewBag.ClientToken = gateway.ClientToken.Generate();
            ViewBag.OrderTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart);

            //set postback values (we cannot access "Form" with "GET" requests)
            if (Request.Method != WebRequestMethods.Http.Get)
                return View("~/Plugins/Payments.BrainTree/Views/PaymentInfo.cshtml", model);

            //var form = Request.Form;
            //model.CardholderName = form["CardholderName"];
            //model.CardNumber = form["CardNumber"];
            //model.CardCode = form["CardCode"];

            //var selectedMonth = model.ExpireMonths
            //    .FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
            //if (selectedMonth != null)
            //    selectedMonth.Selected = true;
            //var selectedYear = model.ExpireYears
            //    .FirstOrDefault(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
            //if (selectedYear != null)
            //    selectedYear.Selected = true;

            return View("~/Plugins/Payments.BrainTree/Views/PaymentInfo.cshtml", model);
        }
    }
}