using System.ComponentModel.DataAnnotations;

namespace SampleApi.Models;

public sealed class SetLogLevelRequest
{
    [Required]
    public string Level { get; init; } = default!;

    public string Category { get; init; }

    [Range(1, 1440)]
    public int DurationMinutes { get; init; } = 15;
}
