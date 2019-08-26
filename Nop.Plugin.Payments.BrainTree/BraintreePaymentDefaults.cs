namespace Nop.Plugin.Payments.BrainTree
{
    public static class BraintreePaymentDefaults
    {
        public static string SystemName => "Payments.BrainTree";

        public static string BraintreeScriptPath => "https://js.braintreegateway.com/v2/braintree.js";

        public static string BraintreeClientScriptPath => "https://js.braintreegateway.com/web/3.50.0/js/client.min.js";

        public static string BraintreeHostedFieldsScriptPath => "https://js.braintreegateway.com/web/3.50.0/js/hosted-fields.min.js";

        public static string Braintree3DSecureScriptPath => "https://js.braintreegateway.com/web/3.50.0/js/three-d-secure.min.js";
    }
}
