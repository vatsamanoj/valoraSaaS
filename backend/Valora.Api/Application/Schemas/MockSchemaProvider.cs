namespace Valora.Api.Application.Schemas
{
    public static class MockSchemaProvider
    {
        public static ModuleSchema Get(string module)
        {
            // Return a minimal valid schema or throw
            return new ModuleSchema(
                TenantId: "system",
                Module: module,
                Version: 1,
                ObjectType: "Master",
                Fields: new Dictionary<string, FieldRule>()
            );
        }
    }
}
