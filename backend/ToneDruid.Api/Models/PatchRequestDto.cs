using System.ComponentModel.DataAnnotations;

namespace ToneDruid.Api.Models;

public sealed class PatchRequestDto
{
    [Required]
    [MaxLength(500)]
    public string? Prompt { get; set; }
}
