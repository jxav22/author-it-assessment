using AuthorIt.Api.Domain;

namespace AuthorIt.Api.Store;

public static class SeedData
{
    // Fixed seed clock so automated checks stay stable.
    public static readonly DateTimeOffset SeededAt =
        new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    public static void Apply(LibraryStore store)
    {
        store.SeedTopic(CreateTopic(
            1,
            "Introduction to Author-it",
            "Author-it stores reusable Topics and assembles Books from references to those Topics.",
            ["overview", "getting-started"]));

        store.SeedTopic(CreateTopic(
            2,
            "Installing the Desktop Client",
            "Install the desktop client on Windows and confirm it can reach your Author-it library.",
            ["installation", "windows", "desktop"]));

        store.SeedTopic(CreateTopic(
            3,
            "Connecting to a Library",
            "Configure library connection settings so authors can open shared content.",
            ["setup", "library", "configuration"]));

        store.SeedTopic(CreateTopic(
            4,
            "Creating Your First Topic",
            "Create a Topic, add body text and tags, then save it for reuse across Books.",
            ["authoring", "topics", "beginner"]));

        store.SeedTopic(CreateTopic(
            5,
            "Publishing to PDF",
            "Publish a Book to PDF using the configured output profile.",
            ["publishing", "output", "pdf"]));

        store.SeedBook(CreateBook(
            1,
            "Getting Started Guide",
            "A short path from installation to your first Topic.",
            [
                Ref(1, topicId: 1, level: 1, position: 1),
                Ref(2, topicId: 2, level: 1, position: 2),
                Ref(3, topicId: 3, level: 2, position: 3),
                Ref(4, topicId: 4, level: 1, position: 4)
            ]));

        store.SeedBook(CreateBook(
            2,
            "Publishing Handbook",
            "How publishing reuses Topics already authored elsewhere.",
            [
                Ref(5, topicId: 1, level: 1, position: 1),
                Ref(6, topicId: 5, level: 1, position: 2),
                Ref(7, topicId: 4, level: 2, position: 3)
            ]));

        store.SeedBook(CreateBook(
            3,
            "Administrator Guide",
            "Administration outline with Topic 3 reused twice.",
            [
                Ref(8, topicId: 2, level: 1, position: 1),
                Ref(9, topicId: 3, level: 2, position: 2),
                Ref(10, topicId: 5, level: 1, position: 3),
                Ref(11, topicId: 3, level: 2, position: 4)
            ]));
    }

    private static Topic CreateTopic(int id, string title, string body, IEnumerable<string> tags) =>
        new()
        {
            Id = id,
            Title = title,
            Body = body,
            Tags = tags.ToList(),
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt,
            Version = 1
        };

    private static Book CreateBook(
        int id,
        string title,
        string? description,
        IEnumerable<BookReference> contents) =>
        new()
        {
            Id = id,
            Title = title,
            Description = description,
            CreatedAt = SeededAt,
            Contents = contents.ToList()
        };

    private static BookReference Ref(int referenceId, int topicId, int level, int position) =>
        new()
        {
            ReferenceId = referenceId,
            TopicId = topicId,
            Level = level,
            Position = position
        };
}
