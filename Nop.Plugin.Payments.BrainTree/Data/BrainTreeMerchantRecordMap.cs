using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Payments.Braintree.Domain;

namespace Nop.Plugin.Payments.Braintree.Data
{
    /// <summary>
    /// Represents a merchant record mapping configuration
    /// </summary>
    public class BraintreeMerchantRecordBuilder : NopEntityBuilder<BraintreeMerchantRecord>
    {
        #region Methods

        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table.WithColumn(nameof(BraintreeMerchantRecord.CurrencyCode)).AsString(5).NotNullable();
        }

        #endregion
    }
}