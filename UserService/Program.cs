using Microsoft.EntityFrameworkCore;
using UserService;
using UserService.Model;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UserContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("AZURE")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserContext>();
    db.Database.Migrate();
}

app.MapPost("/register", async (User user, UserContext ctx) =>
{
    await ctx.AddAsync(user);
    await ctx.SaveChangesAsync();

    return Results.Created("/login", "User was successfully registered!");
});

app.MapPost("/login", async (UserLogin userLogin, UserContext ctx) =>
{
    User? user = await ctx.Users.FirstOrDefaultAsync(u => u.Email.Equals(userLogin.Email) && u.Password.Equals(userLogin.Password));

    if (user == null)
    {
        return Results.NotFound("User not found. Check email or/and password");
    }

    var secretKey = builder.Configuration["Jwt:Key"];

    if (secretKey == null)
    {
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

