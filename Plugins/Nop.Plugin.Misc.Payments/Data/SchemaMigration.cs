using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.Payments.Domain.Quota;

namespace Nop.Plugin.Misc.Payments.Data
{
    [NopMigration("2022/06/30 22:27:24:6455432", "Misc.Payments base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<Quota>();
            Create.TableFor<QuotaDetails>();
        }
    }
}
