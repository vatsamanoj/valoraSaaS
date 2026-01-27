using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Valora.Api.Domain.Common;

namespace Valora.Api.Domain.Entities;

[Table("ObjectDefinition")]
public class ObjectDefinition : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ObjectCode { get; set; } = string.Empty;

    public int Version { get; set; }

    public bool IsActive { get; set; } = true;

    // Optional: Store the full JSON schema here too for reference if needed
    public string? SchemaJson { get; set; }

    public ICollection<ObjectField> ObjectFields { get; set; } = new List<ObjectField>();
}
