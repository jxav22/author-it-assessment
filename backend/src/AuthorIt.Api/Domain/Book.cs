namespace AuthorIt.Api.Domain;

public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<BookReference> Contents { get; set; } = [];
}
