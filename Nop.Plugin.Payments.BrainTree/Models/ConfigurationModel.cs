using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.BrainTree.Models
{
    public class ConfigurationModel : BaseSearchModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BrainTree.Fields.UseSandbox")]
        public bool UseSandBox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BrainTree.Fields.MerchantId")]
        public string MerchantId { get; set; }
        public bool MerchantId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BrainTree.Fields.PublicKey")]
        public string PublicKey { get; set; }
        public bool PublicKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BrainTree.Fields.PrivateKey")]
        public string PrivateKey { get; set; }
        public bool PrivateKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BrainTree.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BrainTree.Fields.UseMultiCurrency")]
        public bool UseMultiCurrency { get; set; }
        public bool UseMultiCurrency_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BrainTree.Fields.Use3DS")]
        public bool Use3DS { get; set; }
        public bool Use3DS_OverrideForStore { get; set; }
    }
}