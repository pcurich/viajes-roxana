using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Payments.PagoEfectivo.Domain;

namespace Nop.Plugin.Payments.PagoEfectivo.Data
{
    [NopMigration("2022/07/05 09:30:17:6455422", "Payments.PagoEfectivo base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<NotificationPagoEfectivo>();
        }
    }
}
