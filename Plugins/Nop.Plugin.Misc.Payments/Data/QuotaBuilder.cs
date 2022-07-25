using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.Payments.Domain.Quota;

namespace Nop.Plugin.Misc.Payments.Data
{
    public class QuotaBuilder : NopEntityBuilder<Quota>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(Quota.OrderTotal))
                .AsDecimal(18, 4);
         }
    }
}
