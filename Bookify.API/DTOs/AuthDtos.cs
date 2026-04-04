using System.ComponentModel.DataAnnotations;

namespace Bookify.API.DTOs;

public class SyncSessionRequest
{
    [Required]
    [MaxLength(128)]
    public string EntraOid { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Name { get; set; }

    [MaxLength(320)]
    public string? Email { get; set; }
}
