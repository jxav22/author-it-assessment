using AuthorIt.Api.Domain;
using AuthorIt.Api.Dtos;
using AuthorIt.Api.Store;

namespace AuthorIt.Api.Services;

public sealed class BookService(LibraryStore store)
{
    public IReadOnlyList<BookSummaryResponse> GetAll() =>
        store.Read(snapshot => snapshot.Books.Select(ToSummary).ToList());

    public BookDetailResponse? GetById(int id) =>
        store.Read(snapshot =>
        {
            var book = snapshot.Books.FirstOrDefault(item => item.Id == id);
            return book is null ? null : ToDetail(book, snapshot.Topics);
        });

    public BookOperationResult Create(CreateBookRequest request)
    {
        var errors = BookValidation.ValidateCreate(request.Title);
        if (errors.Count > 0)
        {
            return BookOperationResult.Invalid(errors);
        }

        var (title, description) = BookValidation.Normalize(request.Title, request.Description);
        var created = store.Write(mutator => mutator.AddBook(title, description, DateTimeOffset.UtcNow));
        return BookOperationResult.Created(ToSummary(created));
    }

    public BookReferenceOperationResult AddReference(int bookId, CreateBookReferenceRequest request)
    {
        return store.Write(mutator =>
        {
            if (!mutator.TryGetBook(bookId, out var book))
            {
                return BookReferenceOperationResult.NotFound($"Book {bookId} was not found.");
            }

            if (!mutator.TryGetTopicTitle(request.TopicId, out var topicTitle))
            {
                return BookReferenceOperationResult.NotFound($"Topic {request.TopicId} was not found.");
            }

            var current = book.Contents.OrderBy(reference => reference.Position).ToList();
            var insertPosition = request.Position ?? current.Count + 1;

            if (insertPosition < 1 || insertPosition > current.Count + 1)
            {
                return BookReferenceOperationResult.Invalid(new Dictionary<string, string[]>
                {
                    ["position"] =
                    [
                        $"Position must be between 1 and {current.Count + 1}."
                    ]
                });
            }

            if (request.Level < 1)
            {
                return BookReferenceOperationResult.Invalid(new Dictionary<string, string[]>
                {
                    ["level"] = ["Level must be at least 1."]
                });
            }

            // Provisional id for outline validation only — allocate for real after validation succeeds
            // so failed inserts leave every store counter untouched.
            var proposed = OutlineValidation.BuildInsertionProposal(
                current,
                request.TopicId,
                request.Level,
                insertPosition,
                referenceId: 0);

            var outlineErrors = OutlineValidation.Validate(proposed);
            if (outlineErrors.Count > 0)
            {
                return BookReferenceOperationResult.Invalid(outlineErrors);
            }

            var referenceId = mutator.AllocateReferenceId();
            proposed[insertPosition - 1].ReferenceId = referenceId;

            var updatedBook = mutator.ReplaceBookContents(bookId, proposed);
            var inserted = updatedBook.Contents.Single(reference => reference.ReferenceId == referenceId);

            return BookReferenceOperationResult.Created(
                new BookReferenceResponse(
                    inserted.ReferenceId,
                    inserted.TopicId,
                    topicTitle,
                    inserted.Level,
                    inserted.Position));
        });
    }

    public BookReferenceOperationResult RemoveReference(int bookId, int referenceId)
    {
        return store.Write(mutator =>
        {
            if (!mutator.TryGetBook(bookId, out var book))
            {
                return BookReferenceOperationResult.NotFound($"Book {bookId} was not found.");
            }

            if (book.Contents.All(reference => reference.ReferenceId != referenceId))
            {
                return BookReferenceOperationResult.NotFound(
                    $"Reference {referenceId} was not found in book {bookId}.");
            }

            var proposed = OutlineValidation.BuildDeletionProposal(book.Contents, referenceId);
            mutator.ReplaceBookContents(bookId, proposed);
            return BookReferenceOperationResult.Deleted();
        });
    }

    private static BookSummaryResponse ToSummary(Book book) =>
        new(book.Id, book.Title, book.Description, book.CreatedAt);

    private static BookDetailResponse ToDetail(Book book, IReadOnlyList<Topic> topics)
    {
        var titles = topics.ToDictionary(topic => topic.Id, topic => topic.Title);
        var contents = book.Contents
            .OrderBy(reference => reference.Position)
            .Select(reference => new BookReferenceResponse(
                reference.ReferenceId,
                reference.TopicId,
                titles.TryGetValue(reference.TopicId, out var title) ? title : string.Empty,
                reference.Level,
                reference.Position))
            .ToList();

        return new BookDetailResponse(
            book.Id,
            book.Title,
            book.Description,
            book.CreatedAt,
            contents);
    }
}

public sealed class BookOperationResult
{
    public bool Succeeded { get; private init; }
    public BookSummaryResponse? Book { get; private init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; private init; }

    public static BookOperationResult Created(BookSummaryResponse book) =>
        new() { Succeeded = true, Book = book };

    public static BookOperationResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new() { Errors = errors };
}

public sealed class BookReferenceOperationResult
{
    public bool Succeeded { get; private init; }
    public bool IsNotFound { get; private init; }
    public bool IsDeleted { get; private init; }
    public string? NotFoundDetail { get; private init; }
    public BookReferenceResponse? Reference { get; private init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; private init; }

    public static BookReferenceOperationResult Created(BookReferenceResponse reference) =>
        new() { Succeeded = true, Reference = reference };

    public static BookReferenceOperationResult Deleted() =>
        new() { Succeeded = true, IsDeleted = true };

    public static BookReferenceOperationResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new() { Errors = errors };

    public static BookReferenceOperationResult NotFound(string detail) =>
        new() { IsNotFound = true, NotFoundDetail = detail };
}
