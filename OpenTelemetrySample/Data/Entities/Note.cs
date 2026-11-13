namespace OpenTelemetrySample.Data.Entities;

public class Note
{
    public Guid Id { get; set; }

    public DateTimeOffset Date { get; set; }

    public required string Text { get; set; }

    public bool Completed { get; set; }
}