using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

using Microsoft.AspNetCore.Identity;

namespace SeedValoraData
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Valora Data Seeding...");

            try
            {
                Console.WriteLine("Connecting to database...");
                var rawConnString = LoadWriteConnectionString();
                
                // Force DNS Resolution to IPv4 if using Pooler (Fix for Mobile Network)
                if (rawConnString.Contains("pooler.supabase.com"))
                {
                    try 
                    {
                        var host = "aws-1-ap-south-1.pooler.supabase.com";
                        var addresses = await Dns.GetHostAddressesAsync(host);
                        var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                        if (ipv4 != null)
                        {
                            rawConnString = rawConnString.Replace(host, ipv4.ToString());
                            Console.WriteLine($"Resolved Pooler to IPv4: {ipv4}");
                        }
                    }
                    catch (Exception ex) 
                    {
                         Console.WriteLine($"DNS Resolution Warning: {ex.Message}");
                    }
                }

                var builder = new NpgsqlConnectionStringBuilder(rawConnString)
                {
                    Timeout = 10,
                    CommandTimeout = 30
                    // SslMode is handled in connection string
                };

                // await PreferIpv6Async(builder); // DISABLED: IPv6 causes hang on mobile network

                using var connection = new NpgsqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                Console.WriteLine("Connected to Supabase (Postgres).");

                await EnsurePlatformTablesAsync(connection);
                
                await EnsureIdentityTablesAsync(connection);
                await SeedIdentityDataAsync(connection);

                await EnsureRemainingTablesAsync(connection);
                await SeedLabTenantAsync(connection);
                await SeedRemainingDataAsync(connection);

                await EnsureModuleTablesFromSchemasAsync(connection);

                await VerifySeedAsync(connection);

                Console.WriteLine("Seeding Completed Successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally 
            {
                Console.WriteLine("Done.");
            }
        }

        static string LoadWriteConnectionString()
        {
            var env = Environment.GetEnvironmentVariable("VALORA_WRITE_CONNECTION");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env;
            }

            var appsettingsPath = FindAppSettingsPath();
            if (appsettingsPath == null)
                throw new FileNotFoundException("Could not locate Valora.Api appsettings.json by walking up parent directories.");

            var json = File.ReadAllText(appsettingsPath);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var cs)
                || !cs.TryGetProperty("WriteConnection", out var writeConn)
                || writeConn.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException("ConnectionStrings:WriteConnection not found in appsettings.json");
            }

            var value = writeConn.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("ConnectionStrings:WriteConnection is empty");
            }

            return value;
        }

        static string? FindAppSettingsPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate1 = Path.Combine(dir.FullName, "Valora", "backend", "Valora.Api", "appsettings.json");
                if (File.Exists(candidate1))
                    return candidate1;

                var candidate2 = Path.Combine(dir.FullName, "backend", "Valora.Api", "appsettings.json");
                if (File.Exists(candidate2))
                    return candidate2;

                dir = dir.Parent;
            }

            return null;
        }

        static async Task PreferIpv6Async(NpgsqlConnectionStringBuilder builder)
        {
            if (string.IsNullOrWhiteSpace(builder.Host) || !builder.Host.Contains("supabase.co", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(builder.Host).WaitAsync(TimeSpan.FromSeconds(3));
                var ipv6 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
                if (ipv6 != null)
                {
                    builder.Host = ipv6.ToString();
                }
            }
            catch
            {
            }
        }

        static async Task EnsurePlatformTablesAsync(IDbConnection db)
        {
            var outbox = await db.ExecuteScalarAsync<string?>("SELECT to_regclass('public.\"OutboxMessages\"')::text;");
            if (outbox == null)
            {
                Console.WriteLine("Table OutboxMessages does not exist. Creating...");
                await db.ExecuteAsync("""
                    CREATE TABLE "OutboxMessages" (
                        "Id" uuid NOT NULL,
                        "TenantId" uuid NOT NULL,
                        "Topic" character varying(200) NOT NULL,
                        "Payload" text NOT NULL,
                        "Status" character varying(50) NOT NULL DEFAULT 'Pending',
                        "Error" text NULL,
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "ProcessedAt" timestamp with time zone NULL,
                        CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id")
                    );
                    CREATE INDEX "IX_OutboxMessages_Status_CreatedAt" ON "OutboxMessages" ("Status", "CreatedAt");
                """);
                Console.WriteLine("Table OutboxMessages created.");
            }
        }

        static async Task EnsureIdentityTablesAsync(IDbConnection db)
        {
            var tenant = await db.ExecuteScalarAsync<string?>("SELECT to_regclass('public.\"Tenant\"')::text;");
            if (tenant == null)
            {
                Console.WriteLine("Table Tenant does not exist. Creating...");
                await db.ExecuteAsync("""
                    CREATE TABLE "Tenant" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "TenantId" character varying(50) NOT NULL,
                        "TenantCode" character varying(50) NOT NULL,
                        "TenantName" character varying(200) NOT NULL,
                        "TenantType" character varying(50) NOT NULL,
                        "Status" character varying(20) NOT NULL,
                        "DatabaseName" character varying(200) NOT NULL,
                        "DatabaseServer" character varying(200) NULL,
                        "PlanCode" character varying(50) NOT NULL,
                        "SubscriptionStart" timestamp with time zone NOT NULL,
                        "SubscriptionEnd" timestamp with time zone NULL,
                        "MaxUsers" integer NULL,
                        "MaxPatients" integer NULL,
                        "MaxMonthlyOrders" integer NULL,
                        "FeaturesJson" text NULL DEFAULT '[]',
                        "RolePermissionsJson" text NULL DEFAULT '[]',
                        "FieldPermissionsJson" text NULL DEFAULT '[]',
                        "ContactEmail" character varying(200) NOT NULL,
                        "ContactPhone" character varying(20) NULL,
                        "Country" character varying(100) NULL,
                        "TimeZone" character varying(100) NULL,
                        "CreatedAt" timestamp with time zone NOT NULL,
                        "CreatedBy" character varying(100) NOT NULL,
                        "UpdatedAt" timestamp with time zone NULL,
                        "UpdatedBy" character varying(100) NULL,
                        CONSTRAINT "PK_Tenant" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine("Table Tenant created.");
            }

            var appUser = await db.ExecuteScalarAsync<string?>("SELECT to_regclass('public.\"AppUser\"')::text;");
            if (appUser == null)
            {
                Console.WriteLine("Table AppUser does not exist. Creating...");
                await db.ExecuteAsync("""
                    CREATE TABLE "AppUser" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "UserType" character varying(30) NOT NULL,
                        "TenantId" character varying(50) NULL,
                        "UserName" character varying(100) NOT NULL,
                        "Email" character varying(200) NOT NULL,
                        "PasswordHash" character varying(500) NOT NULL,
                        "IsActive" boolean NOT NULL,
                        "CreatedAt" timestamp with time zone NOT NULL,
                        "CreatedBy" character varying(100) NOT NULL,
                        "LastLoginAt" timestamp with time zone NULL,
                        CONSTRAINT "PK_AppUser" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine("Table AppUser created.");
            }

            var appRole = await db.ExecuteScalarAsync<string?>("SELECT to_regclass('public.\"AppRole\"')::text;");
            if (appRole == null)
            {
                Console.WriteLine("Table AppRole does not exist. Creating...");
                await db.ExecuteAsync("""
                    CREATE TABLE "AppRole" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "ScopeType" character varying(30) NOT NULL,
                        "TenantId" character varying(50) NULL,
                        "RoleCode" character varying(50) NOT NULL,
                        "RoleName" character varying(100) NOT NULL,
                        "Description" character varying(500) NULL,
                        "IsActive" boolean NOT NULL DEFAULT TRUE,
                        CONSTRAINT "PK_AppRole" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine("Table AppRole created.");
            }

            var userRole = await db.ExecuteScalarAsync<string?>("SELECT to_regclass('public.\"UserRole\"')::text;");
            if (userRole == null)
            {
                Console.WriteLine("Table UserRole does not exist. Creating...");
                await db.ExecuteAsync("""
                    CREATE TABLE "UserRole" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "UserId" bigint NOT NULL,
                        "RoleId" bigint NOT NULL,
                        CONSTRAINT "PK_UserRole" PRIMARY KEY ("Id"),
                        CONSTRAINT "FK_UserRole_AppUser_UserId" FOREIGN KEY ("UserId") REFERENCES "AppUser" ("Id") ON DELETE CASCADE,
                        CONSTRAINT "FK_UserRole_AppRole_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AppRole" ("Id") ON DELETE CASCADE
                    );
                    CREATE INDEX "IX_UserRole_UserId" ON "UserRole" ("UserId");
                    CREATE INDEX "IX_UserRole_RoleId" ON "UserRole" ("RoleId");
                """);
                Console.WriteLine("Table UserRole created.");
            }
        }

        static async Task SeedIdentityDataAsync(IDbConnection db)
        {
            Console.WriteLine("Seeding Identity Data...");

            // 1. Seed System Tenant
            var sysTenant = await db.QueryFirstOrDefaultAsync<dynamic>("SELECT * FROM \"Tenant\" WHERE \"TenantCode\" = 'SYS'");
            if (sysTenant == null)
            {
                await db.ExecuteAsync("""
                    INSERT INTO "Tenant" (
                        "TenantId", "TenantCode", "TenantName", "TenantType", "Status", 
                        "DatabaseName", "PlanCode", "SubscriptionStart", "ContactEmail", 
                        "CreatedAt", "CreatedBy"
                    ) VALUES (
                        'sys-admin', 'SYS', 'System Admin', 'Platform', 'Active', 
                        'postgres', 'PLATFORM', @Now, 'admin@valora.com', 
                        @Now, 'System'
                    )
                """, new { Now = DateTime.UtcNow });
                Console.WriteLine(" - System Tenant seeded.");
            }

            // 2. Seed Platform Admin Role
            var adminRole = await db.QueryFirstOrDefaultAsync<dynamic>("SELECT * FROM \"AppRole\" WHERE \"RoleCode\" = 'PLATFORM_ADMIN'");
            long roleId;
            if (adminRole == null)
            {
                roleId = await db.QuerySingleAsync<long>("""
                    INSERT INTO "AppRole" (
                        "ScopeType", "RoleCode", "RoleName", "Description", "IsActive"
                    ) VALUES (
                        'Platform', 'PLATFORM_ADMIN', 'Platform Administrator', 'Full access to system', TRUE
                    ) RETURNING "Id"
                """);
                Console.WriteLine(" - Platform Admin Role seeded.");
            }
            else
            {
                roleId = (long)adminRole.Id;
            }

            // 3. Seed Admin User
            var adminUser = await db.QueryFirstOrDefaultAsync<dynamic>("SELECT * FROM \"AppUser\" WHERE \"UserName\" = 'admin'");
            long userId;
            if (adminUser == null)
            {
                var hasher = new PasswordHasher<string>();
                string hash = hasher.HashPassword("system", "password"); // Default password

                userId = await db.QuerySingleAsync<long>("""
                    INSERT INTO "AppUser" (
                        "UserType", "TenantId", "UserName", "Email", "PasswordHash", 
                        "IsActive", "CreatedAt", "CreatedBy"
                    ) VALUES (
                        'PlatformAdmin', 'sys-admin', 'admin', 'admin@valora.com', @Hash, 
                        TRUE, @Now, 'System'
                    ) RETURNING "Id"
                """, new { Hash = hash, Now = DateTime.UtcNow });
                Console.WriteLine(" - Admin User seeded (password: password).");
            }
            else
            {
                userId = (long)adminUser.Id;
            }

            // 4. Assign Role
            var userRole = await db.QueryFirstOrDefaultAsync<dynamic>("SELECT * FROM \"UserRole\" WHERE \"UserId\" = @Uid AND \"RoleId\" = @Rid", new { Uid = userId, Rid = roleId });
            if (userRole == null)
            {
                await db.ExecuteAsync("INSERT INTO \"UserRole\" (\"UserId\", \"RoleId\") VALUES (@Uid, @Rid)", new { Uid = userId, Rid = roleId });
                Console.WriteLine(" - Admin User assigned to Platform Admin Role.");
            }
        }

        static async Task EnsureRemainingTablesAsync(IDbConnection db)
        {
            Console.WriteLine("Ensuring remaining tables exist...");

            // 1. AiConfigurations
            if (await TableMissing(db, "AiConfigurations"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "AiConfigurations" (
                        "Id" uuid NOT NULL,
                        "Name" character varying(100) NOT NULL,
                        "Provider" character varying(50) NOT NULL,
                        "Model" character varying(100) NOT NULL,
                        "ApiKey" character varying(255) NULL,
                        "BaseUrl" character varying(255) NULL,
                        "IsActive" boolean NOT NULL,
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        CONSTRAINT "PK_AiConfigurations" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - AiConfigurations created.");
            }

            // 2. AiUsageLogs
            if (await TableMissing(db, "AiUsageLogs"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "AiUsageLogs" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "Timestamp" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "Source" character varying(50) NOT NULL,
                        "RequestType" character varying(100) NOT NULL,
                        "PromptSummary" text NOT NULL,
                        "TokensUsed" integer NOT NULL,
                        "TenantId" character varying(50) NOT NULL,
                        CONSTRAINT "PK_AiUsageLogs" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - AiUsageLogs created.");
            }

            // 3. AiKnowledgeBase
            if (await TableMissing(db, "AiKnowledgeBase"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "AiKnowledgeBase" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "PromptHash" text NOT NULL,
                        "Prompt" text NOT NULL,
                        "Response" text NOT NULL,
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        CONSTRAINT "PK_AiKnowledgeBase" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - AiKnowledgeBase created.");
            }

            // 4. FieldPermission
            if (await TableMissing(db, "FieldPermission"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "FieldPermission" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "FieldPermissionsJson" text NOT NULL DEFAULT '{}',
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "CreatedBy" character varying(100) NOT NULL,
                        "UpdatedAt" timestamp with time zone NULL,
                        "UpdatedBy" character varying(100) NULL,
                        CONSTRAINT "PK_FieldPermission" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - FieldPermission created.");
            }

/*
            // 5. ModuleSchema
            if (await TableMissing(db, "ModuleSchema"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "ModuleSchema" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "TenantId" character varying(50) NOT NULL,
                        "Module" character varying(100) NOT NULL,
                        "Version" integer NOT NULL,
                        "SchemaJson" text NOT NULL,
                        "IsActive" boolean NOT NULL DEFAULT TRUE,
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "CreatedBy" character varying(100) NOT NULL,
                        "UpdatedAt" timestamp with time zone NULL,
                        "UpdatedBy" character varying(100) NULL,
                        CONSTRAINT "PK_ModuleSchema" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - ModuleSchema created.");
            }

            // 6. PlatformObjectTemplate
            if (await TableMissing(db, "PlatformObjectTemplate"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "PlatformObjectTemplate" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "ObjectCode" character varying(100) NOT NULL,
                        "ObjectType" character varying(50) NOT NULL,
                        "Version" integer NOT NULL,
                        "IsPublished" boolean NOT NULL,
                        "SchemaJson" text NOT NULL,
                        "ChangeLog" character varying(500) NULL,
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "CreatedBy" character varying(100) NOT NULL,
                        CONSTRAINT "PK_PlatformObjectTemplate" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - PlatformObjectTemplate created.");
            }
*/

            // 7. PlatformSettings
            if (await TableMissing(db, "PlatformSettings"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "PlatformSettings" (
                        "Key" character varying(200) NOT NULL,
                        "Value" text NOT NULL,
                        "Description" text NULL,
                        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        CONSTRAINT "PK_PlatformSettings" PRIMARY KEY ("Key")
                    );
                """);
                Console.WriteLine(" - PlatformSettings created.");
            }

            // 8. RolePermission
            if (await TableMissing(db, "RolePermission"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "RolePermission" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "RolePermissionsJson" text NOT NULL DEFAULT '{}',
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "CreatedBy" character varying(100) NOT NULL,
                        CONSTRAINT "PK_RolePermission" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - RolePermission created.");
            }

            // 9. SystemErrorLogs
            if (await TableMissing(db, "SystemErrorLogs"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "SystemErrorLogs" (
                        "Id" uuid NOT NULL,
                        "Type" character varying(50) NOT NULL,
                        "IsFixed" boolean NOT NULL,
                        "Data" text NULL,
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "FixedAt" timestamp with time zone NULL,
                        "FixedBy" character varying(100) NULL,
                        CONSTRAINT "PK_SystemErrorLogs" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - SystemErrorLogs created.");
            }

            // 10. SystemErrorFixSteps
            if (await TableMissing(db, "SystemErrorFixSteps"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "SystemErrorFixSteps" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "SystemErrorLogId" uuid NOT NULL,
                        "StepDescription" text NOT NULL,
                        "StepType" character varying(50) NULL,
                        "Status" character varying(20) NULL,
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        CONSTRAINT "PK_SystemErrorFixSteps" PRIMARY KEY ("Id"),
                        CONSTRAINT "FK_SystemErrorFixSteps_SystemErrorLogs_SystemErrorLogId" FOREIGN KEY ("SystemErrorLogId") REFERENCES "SystemErrorLogs" ("Id") ON DELETE CASCADE
                    );
                """);
                Console.WriteLine(" - SystemErrorFixSteps created.");
            }

            // 11. TenantEnvironment
            if (await TableMissing(db, "TenantEnvironment"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "TenantEnvironment" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "TenantId" character varying(50) NOT NULL,
                        "Environment" character varying(20) NOT NULL,
                        "DatabaseName" character varying(200) NOT NULL,
                        "IsActive" boolean NOT NULL DEFAULT TRUE,
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "CreatedBy" character varying(100) NOT NULL,
                        "UpdatedAt" timestamp with time zone NULL,
                        "UpdatedBy" character varying(100) NULL,
                        CONSTRAINT "PK_TenantEnvironment" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - TenantEnvironment created.");
            }

            // 12. TenantFeature
            if (await TableMissing(db, "TenantFeature"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "TenantFeature" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "TenantId" character varying(50) NOT NULL,
                        "FeaturesJson" text NOT NULL DEFAULT '[]',
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "CreatedBy" character varying(100) NOT NULL,
                        "UpdatedAt" timestamp with time zone NULL,
                        "UpdatedBy" character varying(100) NULL,
                        CONSTRAINT "PK_TenantFeature" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - TenantFeature created.");
            }

            // 13. TenantOnboardingLog
            if (await TableMissing(db, "TenantOnboardingLog"))
            {
                await db.ExecuteAsync("""
                    CREATE TABLE "TenantOnboardingLog" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        "TenantId" character varying(50) NOT NULL,
                        "Step" character varying(100) NOT NULL,
                        "Status" character varying(20) NOT NULL,
                        "ErrorMessage" text NULL,
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        CONSTRAINT "PK_TenantOnboardingLog" PRIMARY KEY ("Id")
                    );
                """);
                Console.WriteLine(" - TenantOnboardingLog created.");
            }
        }

        static async Task SeedLabTenantAsync(IDbConnection db)
        {
            Console.WriteLine("Seeding Lab Tenant (LAB003)...");

            var tenantId = "LAB003";
            var tenant = await db.QueryFirstOrDefaultAsync<dynamic>("SELECT * FROM \"Tenant\" WHERE \"TenantCode\" = @Code", new { Code = tenantId });

            if (tenant == null)
            {
                await db.ExecuteAsync("""
                    INSERT INTO "Tenant" (
                        "TenantId", "TenantCode", "TenantName", "TenantType", "Status", 
                        "DatabaseName", "PlanCode", "SubscriptionStart", "ContactEmail", 
                        "CreatedAt", "CreatedBy"
                    ) VALUES (
                        @TId, @TId, 'City Pathology Lab', 'Lab', 'Active', 
                        'postgres', 'PRO', @Now, 'lab003@valora.com', 
                        @Now, 'System'
                    )
                """, new { TId = tenantId, Now = DateTime.UtcNow });
                Console.WriteLine(" - Tenant LAB003 seeded.");
            }
        }

        static async Task SeedRemainingDataAsync(IDbConnection db)
        {
            Console.WriteLine("Seeding remaining data (AI Configs, Features, Settings)...");

            var tenantId = "LAB003";

            // 1. AiConfigurations (System Level)
            var aiConfigCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"AiConfigurations\"");
            if (aiConfigCount == 0)
            {
                await db.ExecuteAsync("""
                    INSERT INTO "AiConfigurations" ("Id", "Name", "Provider", "Model", "ApiKey", "BaseUrl", "IsActive", "CreatedAt", "UpdatedAt")
                    VALUES (@Id, 'Default OpenAI', 'OpenAI', 'gpt-4o', NULL, NULL, TRUE, @Now, @Now)
                """, new { Id = Guid.NewGuid(), Now = DateTime.UtcNow });
                Console.WriteLine(" - AiConfigurations seeded.");
            }

            // 2. PlatformSettings (System Level)
            var settingCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"PlatformSettings\"");
            if (settingCount == 0)
            {
                await db.ExecuteAsync("""
                    INSERT INTO "PlatformSettings" ("Key", "Value", "Description", "UpdatedAt")
                    VALUES 
                    ('DefaultCurrency', 'USD', 'System Default Currency', @Now),
                    ('MaxUploadSize', '10MB', 'Max file upload size', @Now),
                    ('MaintenanceMode', 'false', 'Is system in maintenance', @Now)
                """, new { Now = DateTime.UtcNow });
                Console.WriteLine(" - PlatformSettings seeded.");
            }

            // 3. ModuleSchema (Tenant Level - LAB003)
            var schemaCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"ModuleSchema\" WHERE \"TenantId\" = @TId", new { TId = tenantId });
            if (schemaCount == 0)
            {
                var patientSchema = JsonSerializer.Serialize(new
                {
                    fields = new {
                        Name = new { type = "text", label = "Full Name", required = true },
                        Age = new { type = "number", label = "Age" },
                        Gender = new { type = "select", options = new[] { "Male", "Female", "Other" } }
                    }
                });

                await db.ExecuteAsync("""
                    INSERT INTO "ModuleSchema" ("TenantId", "Module", "Version", "SchemaJson", "IsActive", "CreatedAt", "CreatedBy")
                    VALUES (@TId, 'Patient', 1, @Schema, TRUE, @Now, 'System')
                """, new { TId = tenantId, Schema = patientSchema, Now = DateTime.UtcNow });
                Console.WriteLine(" - ModuleSchema seeded for LAB003.");
            }

            // 4. TenantFeature (Tenant Level - LAB003)
            var featureCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"TenantFeature\" WHERE \"TenantId\" = @TId", new { TId = tenantId });
            if (featureCount == 0)
            {
                var features = JsonSerializer.Serialize(new[] { "LabOrders", "PatientPortal", "SmsNotifications" });

                await db.ExecuteAsync("""
                    INSERT INTO "TenantFeature" ("TenantId", "FeaturesJson", "CreatedAt", "CreatedBy")
                    VALUES (@TId, @Feat, @Now, 'System')
                """, new { TId = tenantId, Feat = features, Now = DateTime.UtcNow });
                Console.WriteLine(" - TenantFeature seeded for LAB003.");
            }

            // 5. TenantEnvironment (Tenant Level - LAB003)
            var envCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"TenantEnvironment\" WHERE \"TenantId\" = @TId", new { TId = tenantId });
            if (envCount == 0)
            {
                await db.ExecuteAsync("""
                    INSERT INTO "TenantEnvironment" ("TenantId", "Environment", "DatabaseName", "IsActive", "CreatedAt", "CreatedBy")
                    VALUES 
                    (@TId, 'PROD', 'lab003_prod', TRUE, @Now, 'System'),
                    (@TId, 'TEST', 'lab003_test', TRUE, @Now, 'System')
                """, new { TId = tenantId, Now = DateTime.UtcNow });
                Console.WriteLine(" - TenantEnvironment seeded for LAB003.");
            }

            // 7. AiKnowledgeBase (System Level)
            var kbCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"AiKnowledgeBase\"");
            if (kbCount == 0)
            {
                await db.ExecuteAsync("""
                    INSERT INTO "AiKnowledgeBase" ("PromptHash", "Prompt", "Response", "CreatedAt")
                    VALUES ('dummy_hash', 'How to create patient?', 'Use the POST /api/patient endpoint.', @Now)
                """, new { Now = DateTime.UtcNow });
                Console.WriteLine(" - AiKnowledgeBase seeded.");
            }

            // 8. FieldPermission (Tenant Level)
            var fpCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"FieldPermission\"");
            if (fpCount == 0)
            {
                await db.ExecuteAsync("""
                    INSERT INTO "FieldPermission" ("FieldPermissionsJson", "CreatedAt", "CreatedBy")
                    VALUES ('{}', @Now, 'System')
                """, new { Now = DateTime.UtcNow });
                Console.WriteLine(" - FieldPermission seeded.");
            }

            // 9. PlatformObjectTemplate (System Level)
            var potCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"PlatformObjectTemplate\"");
            if (potCount == 0)
            {
                await db.ExecuteAsync("""
                    INSERT INTO "PlatformObjectTemplate" ("ObjectCode", "ObjectType", "Version", "IsPublished", "SchemaJson", "CreatedAt", "CreatedBy")
                    VALUES ('Patient', 'Master', 1, TRUE, '{}', @Now, 'System')
                """, new { Now = DateTime.UtcNow });
                Console.WriteLine(" - PlatformObjectTemplate seeded.");
            }

            // 10. RolePermission (System Level)
            var rpCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"RolePermission\"");
            if (rpCount == 0)
            {
                await db.ExecuteAsync("""
                    INSERT INTO "RolePermission" ("RolePermissionsJson", "CreatedAt", "CreatedBy")
                    VALUES ('{}', @Now, 'System')
                """, new { Now = DateTime.UtcNow });
                Console.WriteLine(" - RolePermission seeded.");
            }

            // 11. SystemErrorLogs (System Level)
            var errorCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"SystemErrorLogs\"");
            if (errorCount == 0)
            {
                var errorId = Guid.NewGuid();
                await db.ExecuteAsync("""
                    INSERT INTO "SystemErrorLogs" ("Id", "Type", "IsFixed", "Data", "CreatedAt")
                    VALUES (@Id, 'BackendError', FALSE, '{"message": "Test Error"}', @Now)
                """, new { Id = errorId, Now = DateTime.UtcNow });
                Console.WriteLine(" - SystemErrorLogs seeded.");

                // 12. SystemErrorFixSteps
                await db.ExecuteAsync("""
                    INSERT INTO "SystemErrorFixSteps" ("SystemErrorLogId", "StepDescription", "StepType", "Status", "CreatedAt")
                    VALUES (@ErrId, 'Check logs', 'Manual', 'Pending', @Now)
                """, new { ErrId = errorId, Now = DateTime.UtcNow });
                Console.WriteLine(" - SystemErrorFixSteps seeded.");
            }

            // 13. TenantOnboardingLog
            var onboardingCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM \"TenantOnboardingLog\"");
            if (onboardingCount == 0)
            {
                await db.ExecuteAsync("""
                    INSERT INTO "TenantOnboardingLog" ("TenantId", "Step", "Status", "CreatedAt")
                    VALUES (@TId, 'DatabaseCreation', 'Success', @Now)
                """, new { TId = tenantId, Now = DateTime.UtcNow });
                Console.WriteLine(" - TenantOnboardingLog seeded.");
            }
        }

        static async Task<bool> TableMissing(IDbConnection db, string tableName)
        {
            var result = await db.ExecuteScalarAsync<string?>($"SELECT to_regclass('public.\"{tableName}\"')::text;");
            return result == null;
        }

        static async Task VerifySeedAsync(IDbConnection db)
        {
            Console.WriteLine("Verification completed.");
        }

        static async Task EnsureModuleTablesFromSchemasAsync(IDbConnection db)
        {
            Console.WriteLine("Ensuring per-module data tables from ModuleSchema templates...");

            var rows = await db.QueryAsync<(string Module, string SchemaJson)>(
                "SELECT DISTINCT \"Module\", \"SchemaJson\" FROM \"ModuleSchema\" WHERE \"IsActive\" = TRUE");

            foreach (var row in rows)
            {
                var module = row.Module;
                if (string.IsNullOrWhiteSpace(module))
                    continue;

                if (!await TableMissing(db, module))
                {
                    Console.WriteLine($" - Table {module} already exists.");
                    continue;
                }

                Console.WriteLine($" - Creating table {module} from schema.");
                var sql = BuildModuleCreateTableSql(module, row.SchemaJson);
                await db.ExecuteAsync(sql);
            }
        }

        static string BuildModuleCreateTableSql(string module, string schemaJson)
        {
            var columns = new List<string>
            {
                "\"Id\" uuid NOT NULL",
                "\"TenantId\" character varying(50) NOT NULL",
                "\"CreatedAt\" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc')",
                "\"CreatedBy\" character varying(100) NULL",
                "\"UpdatedAt\" timestamp with time zone NULL",
                "\"UpdatedBy\" character varying(100) NULL"
            };

            try
            {
                using var doc = JsonDocument.Parse(schemaJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("fields", out var fields) &&
                    fields.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in fields.EnumerateObject())
                    {
                        var fieldName = prop.Name;
                        var fieldDef = prop.Value;

                        string? uiType = null;
                        if (fieldDef.TryGetProperty("type", out var typeEl) &&
                            typeEl.ValueKind == JsonValueKind.String)
                        {
                            uiType = typeEl.GetString();
                        }

                        int? maxLength = null;
                        if (fieldDef.TryGetProperty("maxLength", out var maxLenEl) &&
                            maxLenEl.ValueKind == JsonValueKind.Number)
                        {
                            maxLength = maxLenEl.GetInt32();
                        }

                        int? decimalPlaces = null;
                        if (fieldDef.TryGetProperty("decimalPlaces", out var decEl) &&
                            decEl.ValueKind == JsonValueKind.Number)
                        {
                            decimalPlaces = decEl.GetInt32();
                        }

                        bool required = false;
                        if (fieldDef.TryGetProperty("required", out var reqEl) &&
                            reqEl.ValueKind == JsonValueKind.True)
                        {
                            required = true;
                        }

                        string sqlType;
                        var t = uiType?.ToLowerInvariant();

                        if (t == "number")
                        {
                            if (decimalPlaces.HasValue && decimalPlaces.Value > 0)
                            {
                                sqlType = $"numeric(18,{decimalPlaces.Value})";
                            }
                            else
                            {
                                sqlType = "integer";
                            }
                        }
                        else if (t == "date" || t == "datetime")
                        {
                            sqlType = "timestamp with time zone";
                        }
                        else if (t == "checkbox" || t == "boolean")
                        {
                            sqlType = "boolean";
                        }
                        else
                        {
                            var len = maxLength ?? 255;
                            sqlType = $"character varying({len})";
                        }

                        var nullability = required ? " NOT NULL" : " NULL";
                        columns.Add($"\"{fieldName}\" {sqlType}{nullability}");
                    }
                }
            }
            catch
            {
            }

            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"CREATE TABLE \"{module}\" (");
            builder.AppendLine("    " + string.Join(",\n    ", columns) + ",");
            builder.AppendLine($"    CONSTRAINT \"PK_{module}\" PRIMARY KEY (\"Id\")");
            builder.AppendLine(");");

            return builder.ToString();
        }
    }
}
