using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthorIt.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthorIt.Api.Tests;

public sealed class ReferenceHierarchyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public ReferenceHierarchyTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddReference_InvalidBecauseOfFollowingEntry_LeavesOutlineUnchanged()
    {
        var book = await CreateBookAsync("Next-entry indentation fixture");

        // Build a valid 1 → 2 → 3 outline, then try to insert level 1 immediately
        // before the level-3 entry. The insert itself is fine relative to its
        // predecessor, but the shifted following entry (3 after 1) is illegal.
        await AddReferenceAsync(book.Id, topicId: 1, level: 1);
        await AddReferenceAsync(book.Id, topicId: 2, level: 2);
        await AddReferenceAsync(book.Id, topicId: 3, level: 3);

        var before = await GetBookAsync(book.Id);
        Assert.Equal(
            new[] { (1, 1, 1), (2, 2, 2), (3, 3, 3) },
            before.Contents.Select(r => (r.TopicId, r.Level, r.Position)).ToArray());

        var response = await _client.PostAsJsonAsync($"/books/{book.Id}/references", new
        {
            topicId = 4,
            level = 1,
            position = 3
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("level", out _));

        var after = await GetBookAsync(book.Id);
        Assert.Equal(
            before.Contents.Select(r => (r.ReferenceId, r.TopicId, r.Level, r.Position)),
            after.Contents.Select(r => (r.ReferenceId, r.TopicId, r.Level, r.Position)));
    }

    [Fact]
    public async Task TopicRename_IsReflectedInBookDetailAndUsageWithoutRewritingReferences()
    {
        var topicResponse = await _client.PostAsJsonAsync("/topics", new
        {
            title = "Rename source topic",
            body = "Body",
            tags = new[] { "rename" }
        });
        topicResponse.EnsureSuccessStatusCode();
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>(JsonOptions);
        Assert.NotNull(topic);

        var book = await CreateBookAsync("Rename reflection fixture");
        var reference = await AddReferenceAsync(book.Id, topic.Id, level: 1);

        var renamedTitle = "Renamed topic title";
        var updateResponse = await _client.PutAsJsonAsync($"/topics/{topic.Id}", new
        {
            title = renamedTitle,
            body = "Body",
            tags = new[] { "rename" },
            version = topic.Version
        });
        updateResponse.EnsureSuccessStatusCode();

        var detail = await GetBookAsync(book.Id);
        var occurrence = Assert.Single(detail.Contents);
        Assert.Equal(reference.ReferenceId, occurrence.ReferenceId);
        Assert.Equal(topic.Id, occurrence.TopicId);
        Assert.Equal(1, occurrence.Level);
        Assert.Equal(1, occurrence.Position);
        Assert.Equal(renamedTitle, occurrence.TopicTitle);

        var usageResponse = await _client.GetAsync($"/topics/{topic.Id}/usage");
        usageResponse.EnsureSuccessStatusCode();
        var usage = await usageResponse.Content.ReadFromJsonAsync<TopicUsageResponse>(JsonOptions);

        Assert.NotNull(usage);
        Assert.Equal(renamedTitle, usage.TopicTitle);
        Assert.Equal(1, usage.TotalReferences);
        Assert.Equal(1, usage.BookCount);
        Assert.Equal(reference.ReferenceId, Assert.Single(Assert.Single(usage.Books).References).ReferenceId);
    }

    [Fact]
    public async Task UsageCounts_TrackAdditionsAndRemovalsIncludingRepeatsInOneBook()
    {
        var topicResponse = await _client.PostAsJsonAsync("/topics", new
        {
            title = "Usage count topic",
            body = "Body",
            tags = new[] { "usage" }
        });
        topicResponse.EnsureSuccessStatusCode();
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>(JsonOptions);
        Assert.NotNull(topic);

        var bookA = await CreateBookAsync("Usage book A");
        var bookB = await CreateBookAsync("Usage book B");

        var first = await AddReferenceAsync(bookA.Id, topic.Id, level: 1);
        var second = await AddReferenceAsync(bookA.Id, topic.Id, level: 1); // repeat in same book
        await AddReferenceAsync(bookB.Id, topic.Id, level: 1);

        var afterAdds = await GetUsageAsync(topic.Id);
        Assert.Equal(3, afterAdds.TotalReferences);
        Assert.Equal(2, afterAdds.BookCount);
        Assert.Contains(afterAdds.Books, book => book.BookId == bookA.Id && book.References.Count == 2);
        Assert.Contains(afterAdds.Books, book => book.BookId == bookB.Id && book.References.Count == 1);

        var deleteResponse = await _client.DeleteAsync(
            $"/books/{bookA.Id}/references/{first.ReferenceId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await GetUsageAsync(topic.Id);
        Assert.Equal(2, afterDelete.TotalReferences);
        Assert.Equal(2, afterDelete.BookCount); // still used in both books via remaining occurrences
        Assert.Contains(afterDelete.Books, book =>
            book.BookId == bookA.Id &&
            book.References.Count == 1 &&
            book.References[0].ReferenceId == second.ReferenceId);
    }

    private async Task<BookSummaryResponse> CreateBookAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/books", new { title, description = (string?)null });
        response.EnsureSuccessStatusCode();
        var book = await response.Content.ReadFromJsonAsync<BookSummaryResponse>(JsonOptions);
        Assert.NotNull(book);
        return book;
    }

    private async Task<BookDetailResponse> GetBookAsync(int bookId)
    {
        var response = await _client.GetAsync($"/books/{bookId}");
        response.EnsureSuccessStatusCode();
        var book = await response.Content.ReadFromJsonAsync<BookDetailResponse>(JsonOptions);
        Assert.NotNull(book);
        return book;
    }

    private async Task<TopicUsageResponse> GetUsageAsync(int topicId)
    {
        var response = await _client.GetAsync($"/topics/{topicId}/usage");
        response.EnsureSuccessStatusCode();
        var usage = await response.Content.ReadFromJsonAsync<TopicUsageResponse>(JsonOptions);
        Assert.NotNull(usage);
        return usage;
    }

    private async Task<BookReferenceResponse> AddReferenceAsync(int bookId, int topicId, int level)
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
