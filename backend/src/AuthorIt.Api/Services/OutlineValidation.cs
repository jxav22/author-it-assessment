using AuthorIt.Api.Domain;

namespace AuthorIt.Api.Services;

public static class OutlineValidation
{
    public static Dictionary<string, string[]> Validate(IReadOnlyList<BookReference> outline)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (outline.Count == 0)
        {
            return errors;
        }

        if (outline[0].Level != 1)
        {
            errors["level"] = ["The first reference must have level 1."];
        }

        for (var i = 0; i < outline.Count; i++)
        {
            var entry = outline[i];
            if (entry.Level < 1)
            {
                errors["level"] = ["Level must be at least 1."];
                break;
            }

            if (i == 0)
            {
                continue;
            }

            var previous = outline[i - 1];
            if (entry.Level > previous.Level + 1)
            {
                errors["level"] =
                [
                    "A reference may descend by at most one level relative to the previous entry."
                ];
                break;
            }
        }

        return errors;
    }

    public static List<BookReference> BuildInsertionProposal(
        IReadOnlyList<BookReference> current,
        int topicId,
        int level,
        int position,
        int referenceId)
    {
        var proposed = current
            .OrderBy(reference => reference.Position)
            .Select(Clone)
            .ToList();

        proposed.Insert(
            position - 1,
            new BookReference
            {
                ReferenceId = referenceId,
                TopicId = topicId,
                Level = level,
                Position = position
            });

        Renumber(proposed);
        return proposed;
    }

    public static List<BookReference> BuildDeletionProposal(
        IReadOnlyList<BookReference> current,
        int referenceId)
    {
        var ordered = current
            .OrderBy(reference => reference.Position)
            .Select(Clone)
            .ToList();

        var index = ordered.FindIndex(reference => reference.ReferenceId == referenceId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Reference {referenceId} was not found.");
        }

        var removedLevel = ordered[index].Level;
        ordered.RemoveAt(index);

        for (var i = index; i < ordered.Count; i++)
        {
            if (ordered[i].Level <= removedLevel)
            {
                break;
            }

            ordered[i].Level -= 1;
        }

        Renumber(ordered);
        return ordered;
    }

    private static void Renumber(IList<BookReference> outline)
    {
        for (var i = 0; i < outline.Count; i++)
        {
            outline[i].Position = i + 1;
        }
    }

    private static BookReference Clone(BookReference reference) =>
        new()
        {
            ReferenceId = reference.ReferenceId,
            TopicId = reference.TopicId,
            Level = reference.Level,
            Position = reference.Position
        };
}
