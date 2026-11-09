using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthorIt.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthorIt.Api.Tests;

public sealed class BookApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public BookApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBooks_ReturnsSummariesWithoutContents()
    {
        var response = await _client.GetAsync("/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal(3, document.RootElement.GetArrayLength());
        foreach (var book in document.RootElement.EnumerateArray())
        {
            Assert.False(book.TryGetProperty("contents", out _));
            Assert.True(book.TryGetProperty("title", out _));
        }
    }

    [Fact]
    public async Task GetBook_ReturnsOrderedContentsWithDynamicTopicTitles()
    {
        var response = await _client.GetAsync("/books/3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var book = await response.Content.ReadFromJsonAsync<BookDetailResponse>(JsonOptions);

        Assert.NotNull(book);
        Assert.Equal("Administrator Guide", book.Title);
        Assert.Equal(4, book.Contents.Count);
        Assert.Equal(
            new[] { 1, 2, 3, 4 },
            book.Contents.Select(reference => reference.Position).ToArray());
        Assert.Equal(2, book.Contents.Count(reference => reference.TopicId == 3));
        Assert.Contains(book.Contents, reference =>
            reference.TopicId == 3 && reference.TopicTitle == "Connecting to a Library");
    }

    [Fact]
    public async Task TopicUsage_ForSeedTopic3_ReportsThreeReferencesAcrossTwoBooks()
    {
        var response = await _client.GetAsync("/topics/3/usage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var usage = await response.Content.ReadFromJsonAsync<TopicUsageResponse>(JsonOptions);

        Assert.NotNull(usage);
        Assert.Equal(3, usage.TopicId);
        Assert.Equal(3, usage.TotalReferences);
        Assert.Equal(2, usage.BookCount);
        Assert.Contains(usage.Books, book => book.BookId == 1 && book.References.Count == 1);
        Assert.Contains(usage.Books, book => book.BookId == 3 && book.References.Count == 2);
    }

    [Fact]
    public async Task AddReference_WithMissingTopic_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/books/1/references", new
        {
            topicId = 9999,
            level = 1
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddReference_WithInvalidLevelJump_LeavesBookUnchanged()
    {
        var beforeResponse = await _client.GetAsync("/books/1");
        beforeResponse.EnsureSuccessStatusCode();
        var before = await beforeResponse.Content.ReadFromJsonAsync<BookDetailResponse>(JsonOptions);
        Assert.NotNull(before);

        // Inserting level 3 after a level-1 entry at position 2 creates an invalid descent,
        // and also leaves the shifted former position-2 entry invalid relative to the insert.
        var response = await _client.PostAsJsonAsync("/books/1/references", new
        {
            topicId = 5,
            level = 3,
            position = 2
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("level", out _));

        var afterResponse = await _client.GetAsync("/books/1");
        afterResponse.EnsureSuccessStatusCode();
        var after = await afterResponse.Content.ReadFromJsonAsync<BookDetailResponse>(JsonOptions);

        Assert.NotNull(after);
        Assert.Equal(before.Contents.Count, after.Contents.Count);
        Assert.Equal(
            before.Contents.Select(reference => (reference.ReferenceId, reference.TopicId, reference.Level, reference.Position)),
            after.Contents.Select(reference => (reference.ReferenceId, reference.TopicId, reference.Level, reference.Position)));
    }

    [Fact]
    public async Task RemoveReference_PromotesDescendantsAndClosesGaps()
    {
        var createBook = await _client.PostAsJsonAsync("/books", new
        {
            title = "Deletion fixture",
            description = "Temporary outline for delete semantics"
        });
        createBook.EnsureSuccessStatusCode();
        var book = await createBook.Content.ReadFromJsonAsync<BookSummaryResponse>(JsonOptions);
        Assert.NotNull(book);

        await AddReference(book.Id, topicId: 1, level: 1); // pos 1
        var parent = await AddReference(book.Id, topicId: 2, level: 1); // pos 2
        await AddReference(book.Id, topicId: 3, level: 2); // pos 3 child of parent
        await AddReference(book.Id, topicId: 4, level: 3); // pos 4 grandchild
        await AddReference(book.Id, topicId: 5, level: 1); // pos 5 sibling branch

        var deleteResponse = await _client.DeleteAsync(
            $"/books/{book.Id}/references/{parent.ReferenceId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var detailResponse = await _client.GetAsync($"/books/{book.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<BookDetailResponse>(JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal(4, detail.Contents.Count);
        Assert.Equal(
            new[] { (1, 1, 1), (3, 1, 2), (4, 2, 3), (5, 1, 4) },
            detail.Contents.Select(reference => (reference.TopicId, reference.Level, reference.Position)).ToArray());
    }

    private async Task<BookReferenceResponse> AddReference(int bookId, int topicId, int level)
    {
        var response = await _client.PostAsJsonAsync($"/books/{bookId}/references", new
        {
            topicId,
            level
        });
        response.EnsureSuccessStatusCode();
        var reference = await response.Content.ReadFromJsonAsync<BookReferenceResponse>(JsonOptions);
        Assert.NotNull(reference);
        return reference;
    }
}
