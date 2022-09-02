using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Plugin.Payments.SafeTyPay.Domain;

namespace Nop.Plugin.Payments.SafeTyPay.Services
{
    public class NotificationRequestService : INotificationRequestService
    {
        #region Fields

        private readonly IRepository<NotificationRequestTemp> _repository;

        #endregion Fields

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="repository">NotificationRequestTemp</param>
        public NotificationRequestService(IRepository<NotificationRequestTemp> repository)
        {
            _repository = repository;
        }

        #endregion Ctor

        #region Methods

        public virtual async Task DeleteNotificationRequest(NotificationRequestTemp notificationRequestTemp)
        {
            await _repository.DeleteAsync(notificationRequestTemp);
        }

        public virtual async Task<IList<NotificationRequestTemp>> GetAllNotificationRequestTemp()
        {
            var rez = await _repository.GetAllAsync(query =>
            {
                return from req in query select req;
            });
            return rez.ToList();
        }

        public virtual async Task InsertNotificationRequest(NotificationRequestTemp notificationRequestTemp)
        {
            if (notificationRequestTemp == null)
                throw new ArgumentNullException(nameof(notificationRequestTemp));

            await _repository.InsertAsync(notificationRequestTemp);
        }

        public virtual async Task<NotificationRequestTemp> GetNotificationRequestByMerchanId(Guid merchandId)
        {
            var rez = await _repository.GetAllAsync(query =>
            {
                return from req in query
                       where req.MerchantSalesID == merchandId.ToString()
                       select req;
            });

            return rez.FirstOrDefault();
        }

        public virtual async Task UpdateNotificationRequest(NotificationRequestTemp notificationRequestTemp)
        {
            if (notificationRequestTemp == null)
                throw new ArgumentNullException(nameof(notificationRequestTemp));

            await _repository.UpdateAsync(notificationRequestTemp);
        }

        Task<IList<NotificationRequestTemp>> INotificationRequestService.GetAllNotificationRequestTemp()
        {
            throw new NotImplementedException();
        }

        #endregion Methods
    }
}