using AuthorIt.Api.Endpoints;
using AuthorIt.Api.Services;
using AuthorIt.Api.Store;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddSingleton<LibraryStore>();
builder.Services.AddSingleton<TopicService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("FrontendDev");
}

app.MapTopicEndpoints();

app.Run();

public partial class Program;
