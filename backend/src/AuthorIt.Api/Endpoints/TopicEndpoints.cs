using AuthorIt.Api.Dtos;
using AuthorIt.Api.Services;

namespace AuthorIt.Api.Endpoints;

public static class TopicEndpoints
{
    public static RouteGroupBuilder MapTopicEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/topics");

        group.MapGet("/", GetTopics);
        group.MapGet("/{id:int}", GetTopic);
        group.MapPost("/", CreateTopic);
        group.MapPut("/{id:int}", UpdateTopic);

        return group;
    }

    private static IResult GetTopics(TopicService topics) =>
        Results.Ok(topics.GetAll());

    private static IResult GetTopic(int id, TopicService topics)
    {
        var topic = topics.GetById(id);
        return topic is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Topic {id} was not found.",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5")
            : Results.Ok(topic);
    }

    private static IResult CreateTopic(CreateTopicRequest request, TopicService topics)
    {
        var result = topics.Create(request);
        if (result.Errors is not null)
        {
            return Results.ValidationProblem(result.Errors);
        }

        return Results.Created($"/topics/{result.Topic!.Id}", result.Topic);
    }

    private static IResult UpdateTopic(int id, UpdateTopicRequest request, TopicService topics)
    {
        var result = topics.Update(id, request);
        if (result.Errors is not null)
        {
            return Results.ValidationProblem(result.Errors);
        }

        if (result.IsNotFound)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Topic {id} was not found.",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
        }

        if (result.IsConflict)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: "The topic has changed since it was loaded. Reload the latest version and try again.",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.10");
        }

        return Results.Ok(result.Topic);
    }
}
