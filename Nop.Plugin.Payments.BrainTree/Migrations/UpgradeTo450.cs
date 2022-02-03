using System.Collections.Generic;
using FluentMigrator;
using Nop.Core.Infrastructure;
using Nop.Data.Migrations;
using Nop.Services.Localization;

namespace Nop.Plugin.Payments.BrainTree.Migrations
{
    [NopMigration("2022/02/07 16:40:55:1687549", "Payments.Braintree upgrade to 4.50", MigrationProcessType.Update)]
    public class UpgradeTo450 : MigrationBase
    {
        /// <summary>Collect the UP migration expressions</summary>
        public override void Up()
        {
            //do not use DI, because it produces exception on the installation process
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

            localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Payments.Braintree.Errors.3DSecureFailed"] = "The 3D Secure authentication is failed",
                ["Plugins.Payments.Braintree.Errors.ErrorProcessingPayment"] = "Error processing payment."
            });
        }

        /// <summary>Collects the DOWN migration expressions</summary>
        public override void Down()
        {
            //add the downgrade logic if necessary 
        }
    }
}
