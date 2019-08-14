using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.BrainTree.Models
{
    public partial class CurrencyModel : BaseNopEntityModel
    {
        /// <summary>
        /// Gets or sets the currency code
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.BrainTree.Currency.Fields.CurrencyCode")]
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets the merchant account identifier
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.BrainTree.Currency.Fields.MerchantAccountId")]
        public string MerchantAccountId { get; set; }
    }
}
