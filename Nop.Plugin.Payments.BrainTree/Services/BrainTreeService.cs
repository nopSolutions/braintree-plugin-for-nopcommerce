using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.ComponentModel;
using Nop.Data;
using Nop.Plugin.Payments.BrainTree.Domain;
using Nop.Services.Caching;
using Nop.Services.Directory;

namespace Nop.Plugin.Payments.BrainTree.Services
{
    /// <summary>
    /// Represents service shipping by weight service implementation
    /// </summary>
    public partial class BrainTreeService : IBrainTreeService
    {
        #region Constants

        /// <summary>
        /// Key for caching exists currency
        /// </summary>
        /// <remarks>
        /// {0} : store identifier
        /// </remarks>
        private CacheKey BRAINTREESERVICE_EXISTS_CURRENCY_KEY = new CacheKey("Nop.braintree.existscurrencycodes-{0}", BRAINTREESERVICE_EXISTS_CURRENCY_PREFIX);

        private const string BRAINTREESERVICE_EXISTS_CURRENCY_PREFIX = "Nop.braintree.existscurrencycodes";

        /// <summary>
        /// Key for caching merchant
        /// </summary>
        /// <remarks>
        /// {0} : currency code
        /// {1} : store identifier
        /// </remarks>
        private CacheKey BRAINTREESERVICE_MERCHANT_KEY = new CacheKey("Nop.braintree.merchant-{0}-{1}", BRAINTREESERVICE_MERCHANT_PREFIX);

        private const string BRAINTREESERVICE_MERCHANT_PREFIX = "Nop.braintree.merchant-{0}";

        #endregion

        #region Fields

        private readonly ICacheKeyService _cacheKeyService;
        private readonly ICurrencyService _currencyService;
        private readonly IRepository<BrainTreeMerchantRecord> _btmrRepository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreContext _storeContext;
        private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        #endregion

        #region Ctor

        public BrainTreeService(ICacheKeyService cacheKeyService,
            ICurrencyService currencyService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IRepository<BrainTreeMerchantRecord> btmrRepository)
        {
            _cacheKeyService = cacheKeyService;
            _currencyService = currencyService;
            _staticCacheManager = staticCacheManager;
            _storeContext = storeContext;
            _btmrRepository = btmrRepository;
        }

        #endregion

        #region Utilities

        private List<BrainTreeMerchantRecord> GetMerchantsByStoreId(int storeId)
        {
            return _staticCacheManager.Get(
                _cacheKeyService.PrepareKeyForDefaultCache(BRAINTREESERVICE_EXISTS_CURRENCY_KEY, storeId),
                () => { return _btmrRepository.Table.Where(record => record.StoreId == storeId).ToList(); });
        }

        private void UpdateTable(int storeId)
        {
            using (new ReaderWriteLockDisposable(_locker))
            {
                var currencies = _currencyService.GetAllCurrencies(storeId: storeId).ToList();

                foreach (var currency in currencies)
                {
                    if (_btmrRepository.Table.Any(record => record.StoreId == storeId && record.CurrencyCode == currency.CurrencyCode))
                        continue;

                    _btmrRepository.Insert(new BrainTreeMerchantRecord
                    {
                        CurrencyCode = currency.CurrencyCode,
                        MerchantAccountId = string.Empty,
                        StoreId = storeId
                    });

                    _staticCacheManager.RemoveByPrefix(string.Format(BRAINTREESERVICE_MERCHANT_PREFIX, currency.CurrencyCode));
                }

                _staticCacheManager.Remove(_cacheKeyService.PrepareKeyForDefaultCache(BRAINTREESERVICE_EXISTS_CURRENCY_KEY, storeId));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Delete merchants by currency code
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        public void DeleteMerchants(string currencyCode)
        {
            var merchants = _btmrRepository.Table.Where(record => record.CurrencyCode == currencyCode).ToList();

            if (!merchants.Any())
                return;

            foreach (var merchant in merchants)
            {
                _btmrRepository.Delete(merchant);
            }

            _staticCacheManager.RemoveByPrefix(BRAINTREESERVICE_EXISTS_CURRENCY_PREFIX);
            _staticCacheManager.RemoveByPrefix(string.Format(BRAINTREESERVICE_MERCHANT_PREFIX, currencyCode));
        }

        /// <summary>
        /// Get merchant records
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Merchant records</returns>
        public IPagedList<BrainTreeMerchantRecord> GetMerchants(int storeId, int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            UpdateTable(storeId);

            return new PagedList<BrainTreeMerchantRecord>(GetMerchantsByStoreId(storeId), pageIndex, pageSize);
        }

        /// <summary>
        /// Update merchant
        /// </summary>
        /// <param name="id">Merchant record identifier</param>
        /// <param name="merchantAccountId">Merchant account identifier</param>
        public void UpdateMerchant(int id, string merchantAccountId)
        {
            var merchant = _btmrRepository.GetById(id);

            if (merchant == null)
                return;

            merchant.MerchantAccountId = merchantAccountId;

            _btmrRepository.Update(merchant);
            _staticCacheManager.Remove(_cacheKeyService.PrepareKeyForDefaultCache(BRAINTREESERVICE_MERCHANT_KEY, merchant.CurrencyCode, merchant.StoreId));
        }

        /// <summary>
        /// Get merchant identifier 
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        /// <returns>Merchant identifier</returns>
        public string GetMerchantId(string currencyCode)
        {
            var merchant = _staticCacheManager.Get(
                _cacheKeyService.PrepareKeyForDefaultCache(BRAINTREESERVICE_MERCHANT_KEY, currencyCode, _storeContext.CurrentStore.Id), () =>
                {
                    var rez = _btmrRepository.Table.FirstOrDefault(record =>
                        record.CurrencyCode == currencyCode && record.StoreId == _storeContext.CurrentStore.Id);

                    if (string.IsNullOrEmpty(rez?.MerchantAccountId ?? string.Empty))
                        rez = _btmrRepository.Table.FirstOrDefault(record =>
                            record.CurrencyCode == currencyCode && record.StoreId == 0);

                    return rez;
                });

            return merchant?.MerchantAccountId ?? string.Empty;
        }

        #endregion
    }
}
