using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Valora.Api.Domain.Entities;

[Table("ObjectRecordAttribute")]
[Index(nameof(RecordId), nameof(FieldId), IsUnique = true)]
public class ObjectRecordAttribute
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid RecordId { get; set; }

    [ForeignKey("RecordId")]
    public ObjectRecord Record { get; set; } = null!;

    [Required]
    public Guid FieldId { get; set; }

    [ForeignKey("FieldId")]
    public ObjectField Field { get; set; } = null!;

    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public DateTime? ValueDate { get; set; }
    public bool? ValueBoolean { get; set; }
}
