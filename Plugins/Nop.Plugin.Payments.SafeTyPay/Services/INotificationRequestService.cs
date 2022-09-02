using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Payments.SafeTyPay.Domain;

namespace Nop.Plugin.Payments.SafeTyPay.Services
{
    public interface INotificationRequestService
    {
        /// <summary>
        /// Delete a notificationRequestTemp
        /// </summary>
        /// <param name="notificationRequestTemp"></param>
        Task DeleteNotificationRequest(NotificationRequestTemp notificationRequestTemp);

        /// <summary>
        /// Insert a new notificationRequestTemp
        /// </summary>
        /// <param name="notificationRequestTemp"></param>
        Task InsertNotificationRequest(NotificationRequestTemp notificationRequestTemp);

        /// <summary>
        /// Get a list of notificationRequestTemp
        /// </summary>
        /// <returns></returns>
        Task<IList<NotificationRequestTemp>> GetAllNotificationRequestTemp();

        /// <summary>
        /// Get a notificationRequestTemp by MerchanId
        /// </summary>
        /// <param name="merchandId"></param>
        /// <returns></returns>
        Task<NotificationRequestTemp> GetNotificationRequestByMerchanId(Guid merchandId);

        /// <summary>
        /// Update a notificationRequestTemp
        /// </summary>
        /// <param name="notificationRequestTemp"></param>
        Task UpdateNotificationRequest(NotificationRequestTemp notificationRequestTemp);
    }
}