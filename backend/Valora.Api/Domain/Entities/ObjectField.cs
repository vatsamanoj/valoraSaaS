using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Valora.Api.Domain.Common;

namespace Valora.Api.Domain.Entities;

[Table("ObjectField")]
public class ObjectField : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ObjectDefinitionId { get; set; }

    [ForeignKey("ObjectDefinitionId")]
    public ObjectDefinition ObjectDefinition { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = "Text"; // Text, Number, Date, Boolean

    public bool IsRequired { get; set; }
}
