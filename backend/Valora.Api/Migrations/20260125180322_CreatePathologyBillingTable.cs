using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreatePathologyBillingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""pathologybillings"" (
    ""Id"" uuid NOT NULL,
    ""TenantId"" character varying(50) NOT NULL,
    ""BillNo"" character varying(30) NOT NULL,
    ""InvoiceNo"" character varying(50),
    ""BillingDate"" timestamp with time zone NOT NULL,
    ""PatientName"" character varying(120) NOT NULL,
    ""PatientAge"" integer,
    ""PatientGender"" character varying(20),
    ""ContactNumber"" character varying(20),
    ""Email"" character varying(100),
    ""ReferralDoctor"" text,
    ""TotalAmount"" decimal(18,2),
    ""DiscountAmount"" decimal(18,2),
    ""TaxAmount"" decimal(18,2),
    ""NetAmount"" decimal(18,2),
    ""PaymentMode"" character varying(50),
    ""PaymentStatus"" character varying(50),
    ""Remarks"" text,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text,
    ""UpdatedAt"" timestamp with time zone,
    ""UpdatedBy"" text,
    CONSTRAINT ""PK_pathologybillings"" PRIMARY KEY (""Id"")
);

CREATE INDEX IF NOT EXISTS ""IX_pathologybillings_TenantId"" ON ""pathologybillings"" (""TenantId"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_pathologybillings_BillNo"" ON ""pathologybillings"" (""BillNo"") WHERE ""BillNo"" IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"pathologybillings\";");
        }
    }
}
