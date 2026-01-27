namespace Lab360.Application.Schemas
{    
    public interface ISchemaProvider
    {
        Task<ModuleSchema> GetSchemaAsync(
            string tenantId,
            string module,
            CancellationToken cancellationToken);

        void InvalidateCache(string tenantId, string module);
    }
}