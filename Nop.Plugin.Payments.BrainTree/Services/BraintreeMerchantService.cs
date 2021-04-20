using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.ComponentModel;
using Nop.Data;
using Nop.Plugin.Payments.Braintree.Domain;
using Nop.Services.Directory;

namespace Nop.Plugin.Payments.Braintree.Services
{
    /// <summary>
    /// Represents merchant service
    /// </summary>
    public class BraintreeMerchantService
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

        private readonly ICurrencyService _currencyService;
        private readonly IRepository<BraintreeMerchantRecord> _btmrRepository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreContext _storeContext;

        private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        #endregion

        #region Ctor

        public BraintreeMerchantService(ICurrencyService currencyService,
            IRepository<BraintreeMerchantRecord> btmrRepository,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext)
        {
            _currencyService = currencyService;
            _btmrRepository = btmrRepository;
            _staticCacheManager = staticCacheManager;
            _storeContext = storeContext;
        }

        #endregion

        #region Utilities

        private List<BraintreeMerchantRecord> GetMerchantsByStoreId(int storeId)
        {
            return _staticCacheManager.Get(
                _staticCacheManager.PrepareKeyForDefaultCache(BRAINTREESERVICE_EXISTS_CURRENCY_KEY, storeId),
                () => { return _btmrRepository.Table.Where(record => record.StoreId == storeId).ToList(); });
        }

        private void UpdateTable(int storeId)
        {
            //TODO: use async lock
            using (new ReaderWriteLockDisposable(_locker))
            {
                var currencies = _currencyService.GetAllCurrenciesAsync(storeId: storeId).Result.ToList();

                foreach (var currency in currencies)
                {
                    if (_btmrRepository.Table.Any(record => record.StoreId == storeId && record.CurrencyCode == currency.CurrencyCode))
                        continue;

                    _btmrRepository.InsertAsync(new BraintreeMerchantRecord
                    {
                        CurrencyCode = currency.CurrencyCode,
                        MerchantAccountId = string.Empty,
                        StoreId = storeId
                    }).Wait();

                    _staticCacheManager.RemoveByPrefixAsync(string.Format(BRAINTREESERVICE_MERCHANT_PREFIX, currency.CurrencyCode)).Wait();
                }

                _staticCacheManager.RemoveAsync(_staticCacheManager.PrepareKeyForDefaultCache(BRAINTREESERVICE_EXISTS_CURRENCY_KEY, storeId)).Wait();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Delete merchants by currency code
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task DeleteMerchantsAsync(string currencyCode)
        {
            var merchants = _btmrRepository.Table.Where(record => record.CurrencyCode == currencyCode).ToList();

            if (!merchants.Any())
                return;

            await _btmrRepository.DeleteAsync(merchants);

            await _staticCacheManager.RemoveByPrefixAsync(BRAINTREESERVICE_EXISTS_CURRENCY_PREFIX);
            await _staticCacheManager.RemoveByPrefixAsync(string.Format(BRAINTREESERVICE_MERCHANT_PREFIX, currencyCode));
        }

        /// <summary>
        /// Get merchant records
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the merchant records
        /// </returns>
        public IPagedList<BraintreeMerchantRecord> GetMerchants(int storeId, int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            UpdateTable(storeId);

            return new PagedList<BraintreeMerchantRecord>(GetMerchantsByStoreId(storeId), pageIndex, pageSize);
        }

        /// <summary>
        /// Update merchant
        /// </summary>
        /// <param name="id">Merchant record identifier</param>
        /// <param name="merchantAccountId">Merchant account identifier</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task UpdateMerchantAsync(int id, string merchantAccountId)
        {
            var merchant = await _btmrRepository.GetByIdAsync(id);

            if (merchant == null)
                return;

            merchant.MerchantAccountId = merchantAccountId;

            await _btmrRepository.UpdateAsync(merchant);
            await _staticCacheManager.RemoveAsync(_staticCacheManager.PrepareKeyForDefaultCache(BRAINTREESERVICE_MERCHANT_KEY, merchant.CurrencyCode, merchant.StoreId));
        }

        /// <summary>
        /// Get merchant identifier 
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the merchant identifier
        /// </returns>
        public async Task<string> GetMerchantIdAsync(string currencyCode)
        {
            var merchant = await _staticCacheManager.GetAsync(
                _staticCacheManager.PrepareKeyForDefaultCache(BRAINTREESERVICE_MERCHANT_KEY, currencyCode, (await _storeContext.GetCurrentStoreAsync()).Id), () =>
                {
                    var rez = _btmrRepository.Table.FirstOrDefault(record =>
                        record.CurrencyCode == currencyCode && record.StoreId == _storeContext.GetCurrentStore().Id);

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