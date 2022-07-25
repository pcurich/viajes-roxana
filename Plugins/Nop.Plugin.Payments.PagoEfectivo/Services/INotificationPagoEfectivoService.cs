using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Payments.PagoEfectivo.Domain;

namespace Nop.Plugin.Payments.PagoEfectivo.Services
{
    public partial interface INotificationPagoEfectivoService
    {
        /// <summary>
        /// Delete a notificationRequest
        /// </summary>
        /// <param name="notificationRequest"></param>
        Task DeleteNotificationRequestAsync(NotificationPagoEfectivo notificationRequest);

        /// <summary>
        /// Insert a new notificationRequest
        /// </summary>
        /// <param name="notificationRequest"></param>
        Task InsertNotificationRequestAsync(NotificationPagoEfectivo notificationRequest);

        /// <summary>
        /// Get a list of notificationRequest
        /// </summary>
        /// <returns></returns>
        Task<IList<NotificationPagoEfectivo>> GetAllNotificationRequestAsync(int notificationId = 0, int orderId = 0, string cip = "0");

        /// <summary>
        /// Get a notificationRequestTemp by MerchanId
        /// </summary>
        /// <param name="merchandId"></param>
        /// <returns></returns>
        Task<NotificationPagoEfectivo> GetNotificationRequestByCipAsync(string cip);

        /// <summary>
        /// Get Notification Request By Id 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<NotificationPagoEfectivo> GetNotificationRequestByIdAsync(int id);

        /// <summary>
        /// Update a notificationRequest
        /// </summary>
        /// <param name="notificationRequest"></param>
        Task UpdateNotificationRequestAsync(NotificationPagoEfectivo notificationRequest);
    }
}
