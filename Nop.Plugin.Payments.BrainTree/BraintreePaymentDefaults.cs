namespace Nop.Plugin.Payments.Braintree
{
    /// <summary>
    /// Represents plugin constants
    /// </summary>
    public class BraintreePaymentDefaults
    {
        /// <summary>
        /// Gets the plugin system name
        /// </summary>
        public static string SystemName => "Payments.BrainTree";

        /// <summary>
        /// Gets the nopCommerce partner code
        /// </summary>
        public static string PartnerCode => "nopCommerceCart";

        /// <summary>
        /// Gets the one page checkout route name
        /// </summary>
        public static string OnePageCheckoutRouteName => "CheckoutOnePage";

        /// <summary>
        /// Gets the service js script URL
        /// </summary>
        public static string ScriptPath => "https://js.braintreegateway.com/v2/braintree.js";

        /// <summary>
        /// Gets the service client js script URL
        /// </summary>
        public static string ClientScriptPath => "https://js.braintreegateway.com/web/3.62.1/js/client.min.js";

        /// <summary>
        /// Gets the service hosted fields js script URL
        /// </summary>
        public static string HostedFieldsScriptPath => "https://js.braintreegateway.com/web/3.62.1/js/hosted-fields.min.js";

        /// <summary>
        /// Gets the service SCA js script URL
        /// </summary>
        public static string SecureScriptPath => "https://js.braintreegateway.com/web/3.62.1/js/three-d-secure.min.js";
    }
}