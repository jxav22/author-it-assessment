using AuthorIt.Api.Domain;
using AuthorIt.Api.Dtos;
using AuthorIt.Api.Store;

namespace AuthorIt.Api.Services;

public sealed class TopicService(LibraryStore store)
{
    public IReadOnlyList<TopicResponse> GetAll() =>
        store.Read(snapshot => snapshot.Topics.Select(ToResponse).ToList());

    public TopicResponse? GetById(int id) =>
        store.Read(snapshot =>
        {
            var topic = snapshot.Topics.FirstOrDefault(item => item.Id == id);
            return topic is null ? null : ToResponse(topic);
        });

    public TopicOperationResult Create(CreateTopicRequest request)
    {
        var errors = TopicValidation.Validate(request.Title, request.Tags);
        if (errors.Count > 0)
        {
            return TopicOperationResult.Invalid(errors);
        }

        var (title, body, tags) = TopicValidation.Normalize(request.Title, request.Body, request.Tags);
        var created = store.Write(mutator => mutator.AddTopic(title, body, tags, DateTimeOffset.UtcNow));
        return TopicOperationResult.Ok(ToResponse(created));
    }

    public TopicOperationResult Update(int id, UpdateTopicRequest request)
    {
        var errors = TopicValidation.Validate(request.Title, request.Tags);
        if (errors.Count > 0)
        {
            return TopicOperationResult.Invalid(errors);
        }

        var (title, body, tags) = TopicValidation.Normalize(request.Title, request.Body, request.Tags);

        return store.Write(mutator =>
        {
            var updated = mutator.UpdateTopic(
                id,
                request.Version,
                title,
                body,
                tags,
                DateTimeOffset.UtcNow,
                out var conflict);

            if (updated is null)
            {
                return TopicOperationResult.NotFound();
            }

            if (conflict)
            {
                return TopicOperationResult.Conflict();
            }

            return TopicOperationResult.Ok(ToResponse(updated));
        });
    }

    public TopicUsageResponse? GetUsage(int id) =>
        store.Read(snapshot =>
        {
            var topic = snapshot.Topics.FirstOrDefault(item => item.Id == id);
            if (topic is null)
            {
                return null;
            }

            var books = new List<TopicUsageBookResponse>();
            var totalReferences = 0;

            foreach (var book in snapshot.Books.OrderBy(item => item.Id))
            {
                var references = book.Contents
                    .Where(reference => reference.TopicId == id)
                    .OrderBy(reference => reference.Position)
                    .Select(reference => new TopicUsageReferenceResponse(
                        reference.ReferenceId,
                        reference.Level,
                        reference.Position))
                    .ToList();

                if (references.Count == 0)
                {
                    continue;
                }

                totalReferences += references.Count;
                books.Add(new TopicUsageBookResponse(book.Id, book.Title, references));
            }

            return new TopicUsageResponse(
                topic.Id,
                topic.Title,
                books,
                totalReferences,
                books.Count);
        });

    private static TopicResponse ToResponse(Topic topic) =>
        new(
            topic.Id,
            topic.Title,
            topic.Body,
            topic.Tags.ToArray(),
            topic.CreatedAt,
            topic.UpdatedAt,
            topic.Version);
}

public sealed class TopicOperationResult
{
    public bool Succeeded { get; private init; }
    public bool IsNotFound { get; private init; }
    public bool IsConflict { get; private init; }
    public TopicResponse? Topic { get; private init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; private init; }

    public static TopicOperationResult Ok(TopicResponse topic) =>
        new() { Succeeded = true, Topic = topic };

    public static TopicOperationResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new() { Errors = errors };

    public static TopicOperationResult NotFound() =>
        new() { IsNotFound = true };

    public static TopicOperationResult Conflict() =>
        new() { IsConflict = true };
}
