namespace AuthorIt.Api.Dtos;

public sealed record TopicResponse(
    int Id,
    string Title,
    string Body,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version);

public sealed record CreateTopicRequest(
    string? Title,
    string? Body,
    IReadOnlyList<string>? Tags);

public sealed record UpdateTopicRequest(
    string? Title,
    string? Body,
    IReadOnlyList<string>? Tags,
    int Version);
