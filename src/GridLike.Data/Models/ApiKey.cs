using System.ComponentModel.DataAnnotations;

namespace GridLike.Data.Models;

public class ApiKey
{
    [Key]
    public int Id { get; set; }

    [MaxLength(32)] 
    public byte[] Hash { get; set; } = null!;

    [MaxLength(64)] 
    public string Owner { get; set; } = null!;



}