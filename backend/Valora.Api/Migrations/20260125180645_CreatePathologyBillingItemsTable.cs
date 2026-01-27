using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreatePathologyBillingItemsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""pathologybilling_items"" (
    ""Id"" uuid NOT NULL,
    ""ParentId"" uuid NOT NULL,
    ""rowId"" uuid,
    ""TestCode"" character varying(50),
    ""TestName"" character varying(100),
    ""Category"" character varying(50),
    ""Price"" decimal(18,2),
    ""Qty"" integer,
    ""Discount"" decimal(18,2),
    ""TaxPercent"" decimal(18,2),
    ""TaxAmount"" decimal(18,2),
    ""NetAmount"" decimal(18,2),
    ""Attachment"" text,
    CONSTRAINT ""PK_pathologybilling_items"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_pathologybilling_items_pathologybillings_ParentId"" FOREIGN KEY (""ParentId"") REFERENCES ""pathologybillings"" (""Id"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_pathologybilling_items_ParentId"" ON ""pathologybilling_items"" (""ParentId"");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"pathologybilling_items\";");
        }
    }
}
