using Nop.Core;
using Nop.Plugin.Payments.BrainTree.Domain;

namespace Nop.Plugin.Payments.BrainTree.Services
{
    /// <summary>
    /// Represents service shipping by weight service
    /// </summary>
    public partial interface IBrainTreeService
    {
        /// <summary>
        /// Delete merchants by currency code
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        void DeleteMerchants(string currencyCode);

        /// <summary>
        /// Get merchant records
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Merchant records</returns>
        IPagedList<BrainTreeMerchantRecord> GetMerchants(int storeId, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Update merchant
        /// </summary>
        /// <param name="id">Merchant record identifier</param>
        /// <param name="merchantAccountId">Merchant account identifier</param>
        void UpdateMerchant(int id, string merchantAccountId);

        /// <summary>
        /// Get merchant identifier 
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        /// <returns>Merchant identifier</returns>
        string GetMerchantId(string currencyCode);
    }
}
