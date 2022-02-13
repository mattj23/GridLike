using System.ComponentModel.DataAnnotations;

namespace GridLike.Data.Models;

public class JobType
{
    [Key] public int Id { get; set; }

    [MaxLength(32)] public string Name { get; set; } = null!;
    
    [MaxLength(128)] public string? Description { get; set; }
    
    public int? BecomesId { get; set; }
}