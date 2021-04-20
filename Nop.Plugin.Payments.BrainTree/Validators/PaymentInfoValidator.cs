using FluentValidation;
using Nop.Plugin.Payments.Braintree.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.Braintree.Validators
{
    /// <summary>
    /// Represents payment info model validator
    /// </summary>
    public class PaymentInfoValidator : BaseNopValidator<PaymentInfoModel>
    {
        #region Ctor

        public PaymentInfoValidator(BraintreePaymentSettings braintreePaymentSettings, ILocalizationService localizationService)
        {
            if (braintreePaymentSettings.Use3DS)
                return;

            RuleFor(model => model.CardholderName)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Payment.CardholderName.Required"));

            RuleFor(model => model.CardNumber)
                .IsCreditCard()
                .WithMessageAwait(localizationService.GetResourceAsync("Payment.CardNumber.Wrong"));

            RuleFor(model => model.CardCode)
                .Matches(@"^[0-9]{3,4}$")
                .WithMessageAwait(localizationService.GetResourceAsync("Payment.CardCode.Wrong"));

            RuleFor(model => model.ExpireMonth)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Payment.ExpireMonth.Required"));

            RuleFor(model => model.ExpireYear)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Payment.ExpireYear.Required"));
        }

        #endregion
    }
}