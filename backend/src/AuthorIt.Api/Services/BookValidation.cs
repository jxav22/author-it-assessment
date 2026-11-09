namespace AuthorIt.Api.Services;

public static class BookValidation
{
    public const int MaxTitleLength = 200;

    public static Dictionary<string, string[]> ValidateCreate(string? title)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var trimmed = title?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            errors["title"] = ["Title is required."];
        }
        else if (trimmed.Length > MaxTitleLength)
        {
            errors["title"] = [$"Title must be at most {MaxTitleLength} characters."];
        }

        return errors;
    }

    public static (string Title, string? Description) Normalize(string? title, string? description)
    {
        var trimmedDescription = description?.Trim();
        return (
            title?.Trim() ?? string.Empty,
            string.IsNullOrEmpty(trimmedDescription) ? null : trimmedDescription);
    }
}
