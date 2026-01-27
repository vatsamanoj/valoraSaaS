using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEAVTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_extensions");

            migrationBuilder.DropTable(
                name: "PlatformObjectData");

            migrationBuilder.CreateTable(
                name: "ObjectDefinition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ObjectCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SchemaJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectDefinition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObjectField",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectField", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectField_ObjectDefinition_ObjectDefinitionId",
                        column: x => x.ObjectDefinitionId,
                        principalTable: "ObjectDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ObjectDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectRecord_ObjectDefinition_ObjectDefinitionId",
                        column: x => x.ObjectDefinitionId,
                        principalTable: "ObjectDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectRecordAttribute",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueText = table.Column<string>(type: "text", nullable: true),
                    ValueNumber = table.Column<decimal>(type: "numeric", nullable: true),
                    ValueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValueBoolean = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectRecordAttribute", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectRecordAttribute_ObjectField_FieldId",
                        column: x => x.FieldId,
                        principalTable: "ObjectField",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObjectRecordAttribute_ObjectRecord_RecordId",
                        column: x => x.RecordId,
                        principalTable: "ObjectRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectField_ObjectDefinitionId",
                table: "ObjectField",
                column: "ObjectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectRecord_ObjectDefinitionId",
                table: "ObjectRecord",
                column: "ObjectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectRecordAttribute_FieldId",
                table: "ObjectRecordAttribute",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectRecordAttribute_RecordId_FieldId",
                table: "ObjectRecordAttribute",
                columns: new[] { "RecordId", "FieldId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObjectRecordAttribute");

            migrationBuilder.DropTable(
                name: "ObjectField");

            migrationBuilder.DropTable(
                name: "ObjectRecord");

            migrationBuilder.DropTable(
                name: "ObjectDefinition");

            migrationBuilder.CreateTable(
                name: "entity_extensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FieldValue = table.Column<string>(type: "text", nullable: true),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_extensions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformObjectData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    Data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Module = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformObjectData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entity_extensions_EntityId",
                table: "entity_extensions",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_entity_extensions_TenantId_Module_EntityId_FieldKey",
                table: "entity_extensions",
                columns: new[] { "TenantId", "Module", "EntityId", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformObjectData_TenantId_Module",
                table: "PlatformObjectData",
                columns: new[] { "TenantId", "Module" });
        }
    }
}
