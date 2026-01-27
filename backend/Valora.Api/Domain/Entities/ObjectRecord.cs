using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Valora.Api.Domain.Common;

namespace Valora.Api.Domain.Entities;

[Table("ObjectRecord")]
public class ObjectRecord : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    public Guid ObjectDefinitionId { get; set; }

    [ForeignKey("ObjectDefinitionId")]
    public ObjectDefinition ObjectDefinition { get; set; } = null!;
}
