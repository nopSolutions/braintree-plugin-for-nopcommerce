using FluentMigrator;
using Nop.Data.Migrations;
using Nop.Plugin.Payments.Braintree.Domain;

namespace Nop.Plugin.Payments.Braintree.Data
{
    [SkipMigrationOnUpdate]
    [NopMigration("2020/06/02 13:40:55:1687549", "Payments.Braintree base schema")]
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
            _migrationManager.BuildTable<BraintreeMerchantRecord>(Create);
        }

        #endregion
    }
}