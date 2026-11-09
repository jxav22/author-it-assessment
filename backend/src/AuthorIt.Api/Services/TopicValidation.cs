namespace AuthorIt.Api.Services;

public static class TopicValidation
{
    public const int MaxTitleLength = 200;
    public const int MaxTagLength = 50;

    public static Dictionary<string, string[]> Validate(string? title, IReadOnlyList<string>? tags)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length == 0)
        {
            errors["title"] = ["Title is required."];
        }
        else if (trimmedTitle.Length > MaxTitleLength)
        {
            errors["title"] = [$"Title must be at most {MaxTitleLength} characters."];
        }

        var tagErrors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceTags = tags ?? Array.Empty<string>();

        for (var i = 0; i < sourceTags.Count; i++)
        {
            var trimmed = sourceTags[i]?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                tagErrors.Add("Each tag must be non-empty.");
                continue;
            }

            if (trimmed.Length > MaxTagLength)
            {
                tagErrors.Add($"Each tag must be at most {MaxTagLength} characters.");
                continue;
            }

            if (!seen.Add(trimmed))
            {
                tagErrors.Add("Tags must be unique, ignoring case.");
            }
        }

        if (tagErrors.Count > 0)
        {
            errors["tags"] = tagErrors.Distinct(StringComparer.Ordinal).ToArray();
        }

        return errors;
    }

    public static (string Title, string Body, List<string> Tags) Normalize(
        string? title,
        string? body,
        IReadOnlyList<string>? tags)
    {
        var normalizedTags = (tags ?? Array.Empty<string>())
            .Select(tag => tag?.Trim() ?? string.Empty)
            .Where(tag => tag.Length > 0)
            .ToList();

        return (
            title?.Trim() ?? string.Empty,
            body ?? string.Empty,
            normalizedTags);
    }
}
