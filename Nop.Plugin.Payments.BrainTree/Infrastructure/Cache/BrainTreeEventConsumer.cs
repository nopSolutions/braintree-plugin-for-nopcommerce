using Nop.Core.Domain.Directory;
using Nop.Core.Events;
using Nop.Plugin.Payments.BrainTree.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Payments.BrainTree.Infrastructure.Cache
{
    /// <summary>
    /// Event consumer of the "Fixed or by weight" shipping plugin (used for removing unused settings)
    /// </summary>
    public partial class BrainTreeEventConsumer : IConsumer<EntityDeletedEvent<Currency>>
    {
        #region Fields
        
        private readonly IBrainTreeService _brainTreeService;

        #endregion

        #region Ctor

        public BrainTreeEventConsumer(IBrainTreeService brainTreeService)
        {
            _brainTreeService = brainTreeService;
        }

        #endregion

        #region Methods

        public void HandleEvent(EntityDeletedEvent<Currency> eventMessage)
        {
            _brainTreeService.DeleteMerchants(eventMessage.Entity.CurrencyCode);
        }

        #endregion
    }
}