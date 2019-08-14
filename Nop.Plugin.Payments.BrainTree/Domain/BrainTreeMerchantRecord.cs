using Nop.Core;

namespace Nop.Plugin.Payments.BrainTree.Domain
{
    /// <summary>
    /// Represents a shipping by weight record
    /// </summary>
    public partial class BrainTreeMerchantRecord : BaseEntity
    {
        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the currency code
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets the merchant account identifier
        /// </summary>
        public string MerchantAccountId { get; set; }
    }
}