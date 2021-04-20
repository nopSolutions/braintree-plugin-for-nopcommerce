using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Braintree.Models
{
    /// <summary>
    /// Represents a merchant currency model
    /// </summary>
    public record CurrencyModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Plugins.Payments.Braintree.Currency.Fields.CurrencyCode")]
        public string CurrencyCode { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Braintree.Currency.Fields.MerchantAccountId")]
        public string MerchantAccountId { get; set; }
    }
}