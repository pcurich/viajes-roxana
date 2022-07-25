using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.Payments.Domain.Quota;

namespace Nop.Plugin.Misc.Payments.Data
{
    public class QuotaDetailsBuilder : NopEntityBuilder<QuotaDetails>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(QuotaDetails.Amount)).AsDecimal(18, 4)
                .WithColumn(nameof(QuotaDetails.QuotaId)).AsInt32().ForeignKey<Quota>();
        }
    }
}
