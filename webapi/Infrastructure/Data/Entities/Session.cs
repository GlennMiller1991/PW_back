using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webapi.Infrastructure.Data.Entities;

[Table("Sessions")]
public class Session
{
    [Key]
    public int UserId { get; set; }
    
    public required int Version { get; set; }

    public required string RefreshTokenHash { get; set; }
    
    public required DateTime ExpiresAt { get; set; }
    
}