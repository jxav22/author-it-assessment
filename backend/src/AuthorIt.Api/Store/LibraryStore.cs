using AuthorIt.Api.Domain;

namespace AuthorIt.Api.Store;

/// <summary>
/// Singleton in-memory library. All access goes through <see cref="Read"/> / <see cref="Write"/>
/// so version checks and mutations stay atomic.
/// </summary>
public sealed class LibraryStore
{
    private readonly object _gate = new();
    private readonly Dictionary<int, Topic> _topics = new();
    private readonly Dictionary<int, Book> _books = new();
    private int _nextTopicId;
    private int _nextBookId;
    private int _nextReferenceId;

    public LibraryStore()
    {
        SeedData.Apply(this);
    }

    public TResult Read<TResult>(Func<LibrarySnapshot, TResult> query)
    {
        lock (_gate)
        {
            return query(CreateSnapshot());
        }
    }

    public TResult Write<TResult>(Func<LibraryMutator, TResult> command)
    {
        lock (_gate)
        {
            return command(new LibraryMutator(this));
        }
    }

    internal void SeedTopic(Topic topic)
    {
        _topics[topic.Id] = topic;
        _nextTopicId = Math.Max(_nextTopicId, topic.Id + 1);
    }

    internal void SeedBook(Book book)
    {
        _books[book.Id] = book;
        _nextBookId = Math.Max(_nextBookId, book.Id + 1);
        foreach (var reference in book.Contents)
        {
            _nextReferenceId = Math.Max(_nextReferenceId, reference.ReferenceId + 1);
        }
    }

    private LibrarySnapshot CreateSnapshot() =>
        new(
            _topics.Values.OrderBy(topic => topic.Id).Select(CloneTopic).ToList(),
            _books.Values.OrderBy(book => book.Id).Select(CloneBook).ToList());

    private static Topic CloneTopic(Topic topic) =>
        new()
        {
            Id = topic.Id,
            Title = topic.Title,
            Body = topic.Body,
            Tags = [.. topic.Tags],
            CreatedAt = topic.CreatedAt,
            UpdatedAt = topic.UpdatedAt,
            Version = topic.Version
        };

    private static Book CloneBook(Book book) =>
        new()
        {
            Id = book.Id,
            Title = book.Title,
            Description = book.Description,
            CreatedAt = book.CreatedAt,
            Contents = book.Contents
                .OrderBy(reference => reference.Position)
                .Select(reference => new BookReference
                {
                    ReferenceId = reference.ReferenceId,
                    TopicId = reference.TopicId,
                    Level = reference.Level,
                    Position = reference.Position
                })
                .ToList()
        };

    public sealed class LibraryMutator
    {
        private readonly LibraryStore _store;

        internal LibraryMutator(LibraryStore store) => _store = store;

        public Topic AddTopic(string title, string body, List<string> tags, DateTimeOffset now)
        {
            var topic = new Topic
            {
                Id = _store._nextTopicId++,
                Title = title,
                Body = body,
                Tags = [.. tags],
                CreatedAt = now,
                UpdatedAt = now,
                Version = 1
            };
            _store._topics[topic.Id] = topic;
            return CloneTopic(topic);
        }

        public Topic? UpdateTopic(
            int id,
            int expectedVersion,
            string title,
            string body,
            List<string> tags,
            DateTimeOffset now,
            out bool conflict)
        {
            conflict = false;
            if (!_store._topics.TryGetValue(id, out var topic))
            {
                return null;
            }

            if (topic.Version != expectedVersion)
            {
                conflict = true;
                return CloneTopic(topic);
            }

            topic.Title = title;
            topic.Body = body;
            topic.Tags = [.. tags];
            topic.UpdatedAt = now;
            topic.Version++;
            return CloneTopic(topic);
        }

        public bool TopicExists(int topicId) =>
            _store._topics.ContainsKey(topicId);

        public bool TryGetTopicTitle(int topicId, out string title)
        {
            if (_store._topics.TryGetValue(topicId, out var topic))
            {
                title = topic.Title;
                return true;
            }

            title = string.Empty;
            return false;
        }

        public bool TryGetBook(int bookId, out Book book) =>
            _store._books.TryGetValue(bookId, out book!);

        public Book AddBook(string title, string? description, DateTimeOffset now)
        {
            var book = new Book
            {
                Id = _store._nextBookId++,
                Title = title,
                Description = description,
                CreatedAt = now,
                Contents = []
            };
            _store._books[book.Id] = book;
            return CloneBook(book);
        }

        public int AllocateReferenceId() =>
            _store._nextReferenceId++;

        public Book ReplaceBookContents(int bookId, List<BookReference> contents)
        {
            if (!_store._books.TryGetValue(bookId, out var book))
            {
                throw new InvalidOperationException($"Book {bookId} was not found.");
            }

            book.Contents = contents
                .Select(reference => new BookReference
                {
                    ReferenceId = reference.ReferenceId,
                    TopicId = reference.TopicId,
                    Level = reference.Level,
                    Position = reference.Position
                })
                .ToList();

            return CloneBook(book);
        }
    }
}

public sealed record LibrarySnapshot(
    IReadOnlyList<Topic> Topics,
    IReadOnlyList<Book> Books);
