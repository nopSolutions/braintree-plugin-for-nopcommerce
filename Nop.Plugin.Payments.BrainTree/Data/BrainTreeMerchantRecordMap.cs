using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Data.Mapping;
using Nop.Plugin.Payments.BrainTree.Domain;

namespace Nop.Plugin.Payments.BrainTree.Data
{
    /// <summary>
    /// Represents a shipping by weight or by total record mapping configuration
    /// </summary>
    public partial class BrainTreeMerchantRecordMap : NopEntityTypeConfiguration<BrainTreeMerchantRecord>
    {
        #region Methods

        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<BrainTreeMerchantRecord> builder)
        {
            builder.ToTable(nameof(BrainTreeMerchantRecord));
            builder.HasKey(record => record.Id);
            builder.Property(record => record.CurrencyCode).HasMaxLength(5).IsRequired();
        }

        #endregion
    }
}