using System;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.Payments.Models.List;

namespace Nop.Plugin.Misc.Payments.Domain.Quota
{
    public partial class Quota : BaseEntity
    {
        public int OrderId { get; set; }
        public int OrderStatusId { get; set; }
        public int CustomerId { get; set; }
        public int Quotas { get; set; }
        public decimal OrderTotal { get; set; }
        public DateTime StartTimeOnUtc { get; set; }
        public int FrecuencyId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public OrderStatus OrderStatus
        {
            get => (OrderStatus)OrderStatusId;
            set => OrderStatusId = (int)value;
        }

        public FrecuencyList Frecuency
        {
            get => (FrecuencyList)FrecuencyId;
            set => FrecuencyId = (int)value;
        }
    }

}
