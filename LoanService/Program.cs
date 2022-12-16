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

app.MapPost("/loan/{bookId}", async (string bookId, LoanContext ctx, HttpContext httpContext) =>
{
    using var channel = GrpcChannel.ForAddress("http://libraryservice");
    var client = new GetBookService.GetBookServiceClient(channel);

    var bookRequest = new BookRequest
    {
        BookId = bookId
    };

    var book = await client.GetBookAsync(bookRequest);

    if (book == null)
    {
        return Results.NotFound("Book was not found");
    }

    var userId = httpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
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
    var loandbook = await ctx.Loans.FirstOrDefaultAsync(loan => loan.BookId == bookId);
    if (loandbook != null)
    {
        return Results.BadRequest("Book is already loaned");
    };

    await ctx.Loans.AddAsync(loan);
    await ctx.SaveChangesAsync();

    return Results.Created($"/loan/{loan.Id}", "Book has been loaned");
});

app.MapGet("/loans", async (LoanContext ctx, HttpContext httpContext) =>
{
    var userId = httpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier).Value;

    if (userId == null)
    {
        return Results.BadRequest("Bad Token");
    }

    var loans = await ctx.Loans.Where(l => l.UserId.Equals(userId)).ToListAsync();
    return Results.Ok(loans);
});

app.MapPost("/return/{id}", async (string id, LoanContext ctx, HttpContext httpContext) =>
{
    var userId = httpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier).Value;

    if (userId == null)
    {
        return Results.BadRequest("Bad Token");
    }
    Console.WriteLine("USERID: " + userId);
    var loan = await ctx.Loans.Where(l => l.UserId.Equals(userId) && l.BookId.Equals(id)).FirstOrDefaultAsync();

    if (loan == null) return Results.NotFound("You have not loaned this book.");

    ctx.Loans.Remove(loan);

    await ctx.SaveChangesAsync();

    return Results.Ok("Book has been returned");
});

app.Run();

