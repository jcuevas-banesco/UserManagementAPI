using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Logging middleware (optional - built-in logs usually suffice)
app.Use(async (context, next) =>
{
    Console.WriteLine($"[LOG] {context.Request.Method} {context.Request.Path}");
    await next();
});

// Global exception handler middleware
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\": \"An unexpected error occurred.\"}");
    });
});

// In-memory storage for users
var users = new List<User>
{
    new User { Id = 1, Name = "Alice", Email = "alice@example.com" },
    new User { Id = 2, Name = "Bob", Email = "bob@example.com" }
};

// GET all users
app.MapGet("/users", () => users);

// GET user by ID
app.MapGet("/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

// POST new user with validation
app.MapPost("/users", (User newUser) =>
{
    if (string.IsNullOrWhiteSpace(newUser.Name) || 
        string.IsNullOrWhiteSpace(newUser.Email) || 
        !newUser.Email.Contains("@"))
    {
        return Results.BadRequest("Invalid user data: Name and valid Email are required.");
    }

    newUser.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
    users.Add(newUser);
    return Results.Created($"/users/{newUser.Id}", newUser);
});



// PUT update user with validation
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    if (string.IsNullOrWhiteSpace(updatedUser.Name) || 
        string.IsNullOrWhiteSpace(updatedUser.Email) || 
        !updatedUser.Email.Contains("@"))
    {
        return Results.BadRequest("Invalid user data: Name and valid Email are required.");
    }

    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
});

// DELETE user by ID
app.MapDelete("/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    users.Remove(user);
    return Results.NoContent();
});

app.Run();

record User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
}
