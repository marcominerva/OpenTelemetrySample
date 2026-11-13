using System.ComponentModel.DataAnnotations;

namespace OpenTelemetrySample.Models;

public class NoteRequest
{
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Text { get; set; } = string.Empty;

    public bool Completed { get; set; }
}