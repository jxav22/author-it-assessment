using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthorIt.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthorIt.Api.Tests;

public sealed class TopicApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public TopicApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTopics_ReturnsSeededTopics()
    {
        var response = await _client.GetAsync("/topics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var topics = await response.Content.ReadFromJsonAsync<List<TopicResponse>>(JsonOptions);

        Assert.NotNull(topics);
        Assert.Contains(topics, topic => topic.Id == 1 && topic.Title == "Introduction to Author-it");
        Assert.Contains(topics, topic => topic.Id == 3 && topic.Tags.Contains("library"));
        Assert.Equal(5, topics.Count(topic => topic.Id is >= 1 and <= 5));
    }

    [Fact]
    public async Task CreateTopic_WithEmptyTitle_ReturnsFieldError()
    {
        var response = await _client.PostAsJsonAsync("/topics", new
        {
            title = "   ",
            body = "Body",
            tags = new[] { "alpha" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var errors = document.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty("title", out var titleErrors));
        Assert.NotEmpty(titleErrors.EnumerateArray());
    }

    [Fact]
    public async Task CreateTopic_WithDuplicateTags_ReturnsFieldError()
    {
        var response = await _client.PostAsJsonAsync("/topics", new
        {
            title = "Duplicate tags",
            body = "Body",
            tags = new[] { "Guide", "guide" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var errors = document.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty("tags", out var tagErrors));
        Assert.NotEmpty(tagErrors.EnumerateArray());
    }

    [Fact]
    public async Task UpdateTopic_WithStaleVersion_ReturnsConflictAndLeavesDataUnchanged()
    {
        var createResponse = await _client.PostAsJsonAsync("/topics", new
        {
            title = "Concurrency topic",
            body = "Original body",
            tags = new[] { "concurrency" }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<TopicResponse>(JsonOptions);
        Assert.NotNull(created);

        var staleResponse = await _client.PutAsJsonAsync($"/topics/{created.Id}", new
        {
            title = "Stale write",
            body = "Should not persist",
            tags = new[] { "stale" },
            version = created.Version - 1
        });

        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

        var currentResponse = await _client.GetAsync($"/topics/{created.Id}");
        currentResponse.EnsureSuccessStatusCode();
        var current = await currentResponse.Content.ReadFromJsonAsync<TopicResponse>(JsonOptions);

        Assert.NotNull(current);
        Assert.Equal("Concurrency topic", current.Title);
        Assert.Equal("Original body", current.Body);
        Assert.Equal(created.Version, current.Version);
        Assert.Contains("concurrency", current.Tags);
    }

    [Fact]
    public async Task UpdateTopic_WithCurrentVersion_IncrementsVersion()
    {
        var createResponse = await _client.PostAsJsonAsync("/topics", new
        {
            title = "Versioned topic",
            body = "Before",
            tags = new[] { "version" }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<TopicResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(1, created.Version);

        var updateResponse = await _client.PutAsJsonAsync($"/topics/{created.Id}", new
        {
            title = "Versioned topic",
            body = "After",
            tags = new[] { "version" },
            version = created.Version
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TopicResponse>(JsonOptions);

        Assert.NotNull(updated);
        Assert.Equal("After", updated.Body);
        Assert.Equal(created.Version + 1, updated.Version);
    }
}
