using System.Text;
using LibraryService;
using LibraryService.Model;
using LibraryService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };

});

builder.Services.AddGrpc();

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    ctx.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<BookService>();

app.MapPost("/book", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")] async (Book book, LibraryContext ctx) =>
{

    book.Id = Guid.NewGuid();

    await ctx.Books.AddAsync(book);
    await ctx.SaveChangesAsync();

    return Results.Created($"/book/{book.Id}", "Added a new book");
});

app.MapPut("/book/{id}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")] async (string id, Book book, LibraryContext ctx) =>
{

    var idGuid = new Guid(id);

    var bookFromDb = await ctx.Books.FindAsync(idGuid);

    if (bookFromDb == null)
    {
        return Results.NotFound("Book not found");
    }

    bookFromDb.Title = book.Title;

    await ctx.SaveChangesAsync();

    return Results.Ok("Book updated");

});

app.MapDelete("/book/{id}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")] async (string id, LibraryContext ctx) =>
{
    var idGuid = new Guid(id);

    var book = await ctx.Books.FindAsync(idGuid);

    if (book == null)
    {
        return Results.NotFound("Could not finnd book");
    }

    ctx.Books.Remove(book);

    await ctx.SaveChangesAsync();
    return Results.Ok($"{book.Title} was removed successfully!");
});

app.MapGet("/book/{id}", async (string id, LibraryContext ctx) =>
{
    var idGuid = new Guid(id);

    var book = await ctx.Books.FindAsync(idGuid);

    if (book == null)
    {
        return Results.NotFound("Could not find book");
    }

    return Results.Ok(book);
});


app.MapGet("/books", async (LibraryContext ctx) =>
{
    return await ctx.Books.ToListAsync();
});

app.Run();
