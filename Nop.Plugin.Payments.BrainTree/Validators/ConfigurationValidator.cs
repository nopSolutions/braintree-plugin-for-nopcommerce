using FluentValidation;
using Nop.Plugin.Payments.Braintree.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.Braintree.Validators
{
    /// <summary>
    /// Represents configuration model validator
    /// </summary>
    public class ConfigurationValidator : BaseNopValidator<ConfigurationModel>
    {
        #region Ctor

        public ConfigurationValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.MerchantId)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payments.Braintree.Fields.MerchantId.Required"))
                .When(model => !model.UseSandbox);

            RuleFor(model => model.PrivateKey)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payments.Braintree.Fields.PrivateKey.Required"))
                .When(model => !model.UseSandbox);

            RuleFor(model => model.PublicKey)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payments.Braintree.Fields.PublicKey.Required"))
                .When(model => !model.UseSandbox);
        }

        #endregion
    }
}