using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Payments.BrainTree.Domain;

namespace Nop.Plugin.Payments.BrainTree.Data
{
    /// <summary>
    /// Represents a shipping by weight or by total record mapping configuration
    /// </summary>
    public partial class BrainTreeMerchantRecordBuilder : NopEntityBuilder<BrainTreeMerchantRecord>
    {
        #region Methods

        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table.WithColumn(nameof(BrainTreeMerchantRecord.CurrencyCode)).AsString(5).NotNullable();
        }

        #endregion
    }
}