namespace AuthorIt.Api.Dtos;

public sealed record TopicUsageResponse(
    int TopicId,
    string TopicTitle,
    IReadOnlyList<TopicUsageBookResponse> Books,
    int TotalReferences,
    int BookCount);

public sealed record TopicUsageBookResponse(
    int BookId,
    string BookTitle,
    IReadOnlyList<TopicUsageReferenceResponse> References);

public sealed record TopicUsageReferenceResponse(
    int ReferenceId,
    int Level,
    int Position);
