using System.Text;
using LoanService;
using LoanService.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Grpc.Net.Client;
using LoanService.Protos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LoanContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<LibraryClient>(client =>
{
    client.BaseAddress = new Uri("http://libraryservice");
});

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

    options.SaveToken = true;
});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<LoanContext>();
    ctx.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/loan/{bookId}", async (string bookId, LoanContext ctx, LibraryClient libraryClient, HttpContext httpContext) =>
{
    Console.WriteLine("ENTERED");
    using var channel = GrpcChannel.ForAddress("http://libraryservice");
    Console.WriteLine("CHANNEL SUCCESS");
    var client = new GetBookService.GetBookServiceClient(channel);
    Console.WriteLine("GETBOOKSERVICECLIENT SUCCESS");

    var bookRequest = new BookRequest
    {
        BookId = bookId
    };

    var book = await client.GetBookAsync(bookRequest);
    Console.WriteLine("GETBOOKASYNC SUCCESS");

    if (book == null)
    {
        return Results.NotFound("Book was not found");
    }

    var userId = httpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
    Console.WriteLine("USERIDGET SUCCESS");
    if (userId == null)
    {
        return Results.BadRequest("Bad token");
    }

    var loan = new Loan()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        BookId = bookId
    };

    await ctx.Loans.AddAsync(loan);
    await ctx.SaveChangesAsync();

    return Results.Created($"/loan/{loan.Id}", "Book has been loaned");
});

app.MapPost("/return/{id}", async (LoanContext ctx, LibraryClient libraryClient) =>
{





    return Results.Ok();
});



app.Run();

