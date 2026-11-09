namespace AuthorIt.Api.Domain;

public sealed class BookReference
{
    public int ReferenceId { get; set; }
    public int TopicId { get; set; }
    public int Level { get; set; }
    public int Position { get; set; }
}
