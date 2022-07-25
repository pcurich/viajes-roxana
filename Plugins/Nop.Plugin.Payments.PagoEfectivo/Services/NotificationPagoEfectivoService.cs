using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Plugin.Payments.PagoEfectivo.Domain;

namespace Nop.Plugin.Payments.PagoEfectivo.Services
{
    public class NotificationPagoEfectivoService : INotificationPagoEfectivoService
    {
        #region Fields

        private readonly IRepository<NotificationPagoEfectivo> _repository;

        #endregion Fields

        #region Ctor
        public NotificationPagoEfectivoService(IRepository<NotificationPagoEfectivo> repository)
        {
            _repository = repository;
        }


        #endregion

        #region Methods

        public virtual async Task DeleteNotificationRequestAsync(NotificationPagoEfectivo notificationRequest)
        {
            await _repository.DeleteAsync(notificationRequest, false);
        }

        public async Task<IList<NotificationPagoEfectivo>> GetAllNotificationRequestAsync(int notificationId = 0, int orderId=0, string cip = "0" )
        {
            return await _repository.GetAllAsync(query =>
            {
                if (notificationId>0)
                {
                    query = query.Where(entry => entry.Id == notificationId);
                }
                else
                {
                    if (orderId > 0)
                    {
                        query = query.Where(entry => entry.OrderId == orderId);
                    }
                    if (cip != null && cip != "0" )
                    {
                        query = query.Where(entry => entry.Cip == cip);
                    }
                }

                query = query.OrderByDescending(entry => entry.CreatedAt);

                return query;
            });
        }

        public virtual async Task<NotificationPagoEfectivo> GetNotificationRequestByCipAsync(string cip)
        {
            var rez = await _repository.GetAllAsync(query =>
            {
                return from req in query
                       where req.Cip == cip
                       select req; 
            });

            return rez.FirstOrDefault();
        }

        public virtual async Task<NotificationPagoEfectivo> GetNotificationRequestByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public virtual async Task InsertNotificationRequestAsync(NotificationPagoEfectivo notificationRequest)
        {
            await _repository.InsertAsync(notificationRequest,false);
        }

        public virtual async Task UpdateNotificationRequestAsync(NotificationPagoEfectivo notificationRequest)
        {
            await _repository.UpdateAsync(notificationRequest, false);
        }

        #endregion
    }
}
