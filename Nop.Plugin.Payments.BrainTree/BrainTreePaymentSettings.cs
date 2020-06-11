using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Braintree
{
    /// <summary>
    /// Represents plugin settings
    /// </summary>
    public class BraintreePaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox environment
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Gets or sets a merchant identifier
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        /// Gets or sets a public key
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets a private key
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use multi-currency setup
        /// </summary>
        public bool UseMultiCurrency { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use Strong Customer Authentication (SCA) with 3-D secure implementation
        /// </summary>
        public bool Use3DS { get; set; }
    }
}