namespace Valora.Api.Application.Schemas
{    
    public interface ISchemaProvider
    {
        Task<ModuleSchema> GetSchemaAsync(
            string tenantId,
            string module,
            CancellationToken cancellationToken,
            int? version = null);

        Task SeedSchemaAsync(string tenantId, string module, CancellationToken cancellationToken); // 🔥 NEW

        void InvalidateCache(string tenantId, string module);
    }
}