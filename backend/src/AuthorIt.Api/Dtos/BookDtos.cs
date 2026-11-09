namespace AuthorIt.Api.Dtos;

public sealed record BookSummaryResponse(
    int Id,
    string Title,
    string? Description,
    DateTimeOffset CreatedAt);

public sealed record BookReferenceResponse(
    int ReferenceId,
    int TopicId,
    string TopicTitle,
    int Level,
    int Position);

public sealed record BookDetailResponse(
    int Id,
    string Title,
    string? Description,
    DateTimeOffset CreatedAt,
    IReadOnlyList<BookReferenceResponse> Contents);

public sealed record CreateBookRequest(
    string? Title,
    string? Description);

public sealed record CreateBookReferenceRequest(
    int TopicId,
    int Level,
    int? Position);
