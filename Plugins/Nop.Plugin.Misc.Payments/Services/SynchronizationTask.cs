using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Misc.Payments.Services
{
    public class SynchronizationTask : IScheduleTask
    {
        #region Fields

        private readonly CustomQuotaManager _customQuotaManager;

        #endregion

        #region Ctor

        public SynchronizationTask(CustomQuotaManager customQuotaManager)
        {
            _customQuotaManager = customQuotaManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Execute task
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ExecuteAsync()
        {
            await _customQuotaManager.SyncCustomQuotaAsync();
        }

        #endregion
    }
}
