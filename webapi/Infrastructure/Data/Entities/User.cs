using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webapi.Infrastructure.Data.Entities;

[Table("Users")]
public class User
{
    public int Id { get; set; }

    [StringLength(255)] 
    public string GoogleId { get; set; }
}