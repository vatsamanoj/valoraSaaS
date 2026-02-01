using System.ComponentModel.DataAnnotations;

namespace Valora.Api.Domain.Common;

public abstract class AuditableEntity : IAggregateRoot
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Application-managed Optimistic Concurrency Control.
    /// Auto-incremented by AutoIncrementVersionInterceptor.
    /// </summary>
    [ConcurrencyCheck]
    public uint Version { get; set; }
}
