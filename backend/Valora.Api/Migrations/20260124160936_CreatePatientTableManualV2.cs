using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreatePatientTableManualV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""patients"" (
    ""Id"" uuid NOT NULL,
    ""TenantId"" character varying(50) NOT NULL,
    ""Name"" character varying(100) NOT NULL,
    ""Age"" integer,
    ""Gender"" character varying(20),
    ""Uhid"" character varying(50),
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text,
    ""UpdatedAt"" timestamp with time zone,
    ""UpdatedBy"" text,
    CONSTRAINT ""PK_patients"" PRIMARY KEY (""Id"")
);

CREATE INDEX IF NOT EXISTS ""IX_patients_TenantId"" ON ""patients"" (""TenantId"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_patients_Uhid"" ON ""patients"" (""Uhid"") WHERE ""Uhid"" IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_patients_TenantId_Uhid"" ON ""patients"" (""TenantId"", ""Uhid"") WHERE ""Uhid"" IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"patients\";");
        }
    }
}
