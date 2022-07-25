using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Misc.Payments.Domain.Quota;

namespace Nop.Plugin.Misc.Payments.Services
{
    public partial class QuotaService : IQuotaService
    {
        #region Fields

        private readonly IRepository<Quota> _repositoryQuota;
        private readonly IRepository<QuotaDetails> _repositoryQuotaDetails;
        private readonly IRepository<OrderNote> _repositoryOrderNote;

        #endregion

        #region Ctor

        public QuotaService(IRepository<Quota> repositoryQuota, IRepository<QuotaDetails> repositoryQuotaDetails, IRepository<OrderNote> repositoryOrderNote)
        {
            _repositoryQuota = repositoryQuota;
            _repositoryQuotaDetails = repositoryQuotaDetails;
            _repositoryOrderNote = repositoryOrderNote;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Deletes a tax rate
        /// </summary>
        /// <param name="taxRate">Tax rate</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task DeleteQuotaAsync(Quota quota)
        {
            await _repositoryQuota.DeleteAsync(quota);
        }

        /// <summary>
        /// Gets all tax rates
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the ax rates
        /// </returns>
        public virtual async Task<IPagedList<Quota>> GetAllQuotaAsync(int orderId = 0, int orderStatusId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var rez = await _repositoryQuota.GetAllAsync(query =>
            {
                if (orderId > 0 && orderStatusId == 0)
                    query = query.Where(c => c.OrderId == orderId);
                if (orderStatusId > 0 && orderId == 0)
                    query = query.Where(c => c.OrderStatusId == orderStatusId);

                query = query.OrderByDescending(c => c.CreatedOnUtc);

                return query;

            });

            var records = new PagedList<Quota>(rez, pageIndex, pageSize);

            return records;
        }

        /// <summary>
        /// Gets a tax rate
        /// </summary>
        /// <param name="quotaId">quota identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the ax rate
        /// </returns>
        public virtual async Task<Quota> GetQuotaByIdAsync(int quotaId)
        {
            return await _repositoryQuota.GetByIdAsync(quotaId);
        }

        /// <summary>
        /// Gets a tax rate
        /// </summary>
        /// <param name="quotaId">quota identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the ax rate
        /// </returns>
        public virtual async Task<Quota> GetQuotaByOrderId(int orderId)
        {

            var query = from c in _repositoryQuota.Table
                        where c.OrderId == orderId
                        orderby c.Id
                        select c;
            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Inserts a tax rate
        /// </summary>
        /// <param name="taxRate">Tax rate</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task InsertQuotaAsync(Quota quota)
        {
            await _repositoryQuota.InsertAsync(quota);
        }

        /// <summary>
        /// Updates the tax rate
        /// </summary>
        /// <param name="taxRate">Tax rate</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task UpdateQuotaAsync(Quota quota)
        {
            await _repositoryQuota.UpdateAsync(quota);
        }


        /// <summary>
        /// Inserts a tax rate
        /// </summary>
        /// <param name="taxRate">Tax rate</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task InsertQuotaDetailsAsync(QuotaDetails quotaDetails)
        {
            await _repositoryQuotaDetails.InsertAsync(quotaDetails);
        }

        /// <summary>
        /// Gets all tax rates
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the ax rates
        /// </returns>
        public virtual async Task<IPagedList<QuotaDetails>> GetAllQuotaDetailsAsync(int quotaId = 0)
        {
            var rez = await _repositoryQuotaDetails.GetAllAsync(query =>
            {
                query = query.Where(c => c.QuotaId == quotaId);
                query = query.OrderBy(c => c.Sequence);
                return query;

            });

            var records = new PagedList<QuotaDetails>(rez, 0, 100);

            return records;
        }

        public virtual async Task<QuotaDetails> GetQuotaDetailsByIdAsync(int id)
        {
            return await _repositoryQuotaDetails.GetByIdAsync(id);
        }

        public virtual async Task UpdateQuotaDetailsAsync(QuotaDetails quotaDetails)
        {
            await _repositoryQuotaDetails.UpdateAsync(quotaDetails);
        }

        public virtual async Task UpdateOrderNoteAsync(OrderNote orderNote)
        {
            await _repositoryOrderNote.UpdateAsync(orderNote);
        }

        #endregion
    }
}
