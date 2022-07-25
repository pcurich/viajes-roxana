using System.Threading.Tasks;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Payments.PagoEfectivo.Services
{
    /// <summary>
    /// Represents a schedule task to payment payed
    /// </summary>
    public class SynchronizationTask:IScheduleTask
    {
        #region Fields

        private readonly PagoEfectivoManager _pagoEfectivoManager;

        #endregion

        #region Ctor

        public SynchronizationTask(PagoEfectivoManager pagoEfectivoManager)
        {
            _pagoEfectivoManager = pagoEfectivoManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Execute task
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ExecuteAsync()
        {
            await _pagoEfectivoManager.SyncPaymentAsync();
        }

        #endregion
    }
}
