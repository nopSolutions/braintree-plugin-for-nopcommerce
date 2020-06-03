using FluentMigrator;
using Nop.Data.Migrations;
using Nop.Plugin.Payments.BrainTree.Domain;

namespace Nop.Plugin.Payments.BrainTree.Data
{
    [SkipMigrationOnUpdate]
    [NopMigration("2020/06/02 13:40:55:1687549", "Payments.BrainTree base schema")]
    public class SchemaMigration : AutoReversingMigration
    {
        protected IMigrationManager _migrationManager;

        public SchemaMigration(IMigrationManager migrationManager)
        {
            _migrationManager = migrationManager;
        }

        public override void Up()
        {
            _migrationManager.BuildTable<BrainTreeMerchantRecord>(Create);
        }
    }
}