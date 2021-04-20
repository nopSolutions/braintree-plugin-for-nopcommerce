using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Braintree.Models
{
    /// <summary>
    /// Represents a merchant currency list model
    /// </summary>
    public record CurrencyListModel : BasePagedListModel<CurrencyModel>
    {
    }
}