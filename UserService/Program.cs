using Microsoft.EntityFrameworkCore;
using UserService;
using UserService.Model;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UserContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<UserContext>();
    ctx.Database.Migrate();
}

app.MapPost("/register", async (User user, UserContext ctx) =>
{
    user.Id = Guid.NewGuid();
    Console.WriteLine($"NEW USER ADDED: {user.Id} {user.Email} {user.Password} {user.Role}");

    await ctx.Users.AddAsync(user);
    await ctx.SaveChangesAsync();

    return Results.Created("/login", "User was successfully registered!");
});

app.MapPost("/login", async (UserLogin userLogin, UserContext ctx) =>
{
    Console.WriteLine($"USER TO LOG IN: {userLogin.Email} {userLogin.Password}");
    User? user = await ctx.Users.FirstOrDefaultAsync(u => u.Email.Equals(userLogin.Email) && u.Password.Equals(userLogin.Password));

    if (user == null)
    {
        return Results.NotFound("User not found. Check email or/and password");
    }

    var secretKey = builder.Configuration["Jwt:Key"];

    if (secretKey == null)
    {
        Console.WriteLine("KEY IS NULL");
        return Results.StatusCode(500);
    }

    var claims = new[]{
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var token = new JwtSecurityToken(
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(30),
        notBefore: DateTime.Now,
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256)
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(tokenString);
});



app.Run();

