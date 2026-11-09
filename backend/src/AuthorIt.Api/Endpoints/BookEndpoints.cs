using AuthorIt.Api.Dtos;
using AuthorIt.Api.Services;

namespace AuthorIt.Api.Endpoints;

public static class BookEndpoints
{
    public static RouteGroupBuilder MapBookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/books");

        group.MapGet("/", GetBooks);
        group.MapGet("/{id:int}", GetBook);
        group.MapPost("/", CreateBook);
        group.MapPost("/{id:int}/references", AddReference);
        group.MapDelete("/{id:int}/references/{referenceId:int}", RemoveReference);

        return group;
    }

    private static IResult GetBooks(BookService books) =>
        Results.Ok(books.GetAll());

    private static IResult GetBook(int id, BookService books)
    {
        var book = books.GetById(id);
        return book is null
            ? NotFound($"Book {id} was not found.")
            : Results.Ok(book);
    }

    private static IResult CreateBook(CreateBookRequest request, BookService books)
    {
        var result = books.Create(request);
        if (result.Errors is not null)
        {
            return Results.ValidationProblem(result.Errors);
        }

        return Results.Created($"/books/{result.Book!.Id}", result.Book);
    }

    private static IResult AddReference(
        int id,
        CreateBookReferenceRequest request,
        BookService books)
    {
        var result = books.AddReference(id, request);
        if (result.Errors is not null)
        {
            return Results.ValidationProblem(result.Errors);
        }

        if (result.IsNotFound)
        {
            return NotFound(result.NotFoundDetail ?? $"Book {id} was not found.");
        }

        return Results.Created(
            $"/books/{id}/references/{result.Reference!.ReferenceId}",
            result.Reference);
    }

    private static IResult RemoveReference(int id, int referenceId, BookService books)
    {
        var result = books.RemoveReference(id, referenceId);
        if (result.IsNotFound)
        {
            return NotFound(result.NotFoundDetail ?? $"Reference {referenceId} was not found.");
        }

        return Results.NoContent();
    }

    private static IResult NotFound(string detail) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: detail,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
}
