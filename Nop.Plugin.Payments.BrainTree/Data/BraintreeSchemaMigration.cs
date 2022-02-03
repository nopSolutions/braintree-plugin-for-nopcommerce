using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Payments.Braintree.Domain;

namespace Nop.Plugin.Payments.Braintree.Data
{
    [NopMigration("2020/06/02 13:40:55:1687549", "Payments.Braintree base schema", MigrationProcessType.Installation)]
    public class BraintreeSchemaMigration : AutoReversingMigration
    {
        #region Fields

        protected IMigrationManager _migrationManager;

        #endregion

        #region Ctor

        public BraintreeSchemaMigration(IMigrationManager migrationManager)
        {
            _migrationManager = migrationManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Collect the UP migration expressions
        /// </summary>
        public override void Up()
        {
            Create.TableFor<BraintreeMerchantRecord>();
        }

        #endregion
    }
}