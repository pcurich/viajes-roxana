using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.Payments.Domain.Quota;

namespace Nop.Plugin.Misc.Payments.Services
{
    public partial interface IQuotaService
    {
        #region Quota

        /// <summary>
        /// Delete a Quota a Async
        /// </summary>
        /// <param name="quota"></param>
        /// <returns></returns>
        Task DeleteQuotaAsync(Quota quota);

        /// <summary>
        /// Gets all quotas
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains quotas
        /// </returns>
        Task<IPagedList<Quota>> GetAllQuotaAsync(int orderId = 0, int orderStatusId = 0, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets a quota
        /// </summary>
        /// <param name="taxRateId">Quota identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains quota
        /// </returns>
        Task<Quota> GetQuotaByIdAsync(int quotaId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        Task<Quota> GetQuotaByOrderId(int orderId);

        /// <summary>
        /// Inserts a quota
        /// </summary>
        /// <param name="quota">Quota</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task InsertQuotaAsync(Quota quota);

        /// <summary>
        /// Updates quota
        /// </summary>
        /// <param name="quota">quota</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task UpdateQuotaAsync(Quota quota);

        #endregion

        #region QuotaDetails

        /// <summary>
        /// Insert Quota Detail Async
        /// </summary>
        /// <param name="quota"></param>
        /// <returns></returns>
        Task InsertQuotaDetailsAsync(QuotaDetails quota);

        /// <summary>
        /// Gets all quotas
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains quotas
        /// </returns>
        Task<IPagedList<QuotaDetails>> GetAllQuotaDetailsAsync(int quotaId = 0);

        /// <summary>
        /// Gets a quota
        /// </summary>
        /// <param name="taxRateId">Quota identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains quota
        /// </returns>
        Task<QuotaDetails> GetQuotaDetailsByIdAsync(int id);

        /// <summary>
        /// Updates quota
        /// </summary>
        /// <param name="quota">quota</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task UpdateQuotaDetailsAsync(QuotaDetails quotaDetails);

        /// <summary>
        /// Update Order Note Async
        /// </summary>
        /// <param name="orderNote"></param>
        /// <returns></returns>
        Task UpdateOrderNoteAsync(OrderNote orderNote);
        #endregion

    }
}
