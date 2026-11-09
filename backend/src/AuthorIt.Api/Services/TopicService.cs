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
