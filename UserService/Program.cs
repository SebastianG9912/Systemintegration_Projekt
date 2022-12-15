using Microsoft.EntityFrameworkCore;
using UserService;
using UserService.Model;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UserContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<UserContext>();
    ctx.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/register", async (User user, UserContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Password))
    {
        return Results.BadRequest("Please include email and password!");
    }

    var existingUser = await ctx.Users.FirstOrDefaultAsync(u => u.Email.Equals(user.Email));

    if (existingUser != null)
    {
        return Results.Conflict($"User with email: {user.Email} already exists!");
    }

    user.Id = Guid.NewGuid();

    await ctx.Users.AddAsync(user);
    await ctx.SaveChangesAsync();

    return Results.Created("/login", $"User with email: {user.Email} was successfully registered!");
});

app.MapPost("/login", async (UserLogin userLogin, UserContext ctx) =>
{
    var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email.Equals(userLogin.Email) && u.Password.Equals(userLogin.Password));

    if (user == null)
    {
        return Results.BadRequest("User not found. Check email or/and password");
    }

    if (user.Blacklisted)
    {
        return Results.Forbid();
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

app.MapGet("/users", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")] async (UserContext ctx) =>
{
    return await ctx.Users.ToListAsync();
});

app.MapPut("/user/{userId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")] async (String userId, User user, UserContext ctx) =>
{
    var userFromDb = await ctx.Users.FirstOrDefaultAsync(u => u.Id.ToString().Equals(userId));

    if (userFromDb == null)
    {
        return Results.BadRequest($"User with id: {userId} does not exist.");
    }

    if (!String.IsNullOrWhiteSpace(user.Email))
    {
        userFromDb.Email = user.Email;
    }

    if (!String.IsNullOrWhiteSpace(user.Password))
    {
        userFromDb.Password = user.Password;
    }

    userFromDb.Role = user.Role;

    await ctx.SaveChangesAsync();

    return Results.Ok($"Updated properties of user with email: {user.Email}");
});

app.MapPut("/blacklist/{userId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")] async (string userId, UserContext ctx) =>
{
    var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id.ToString().Equals(userId));

    if (user == null)
    {
        return Results.BadRequest($"User with id: {userId} does not exist.");
    }

    var responseText = $"User with email: {user.Email} has been blacklisted!";
    if (user.Blacklisted)
    {
        user.Blacklisted = false;
        responseText = $"User with email: {user.Email} has been removed from blacklist!";
    }
    else
    {
        user.Blacklisted = true;
    }

    await ctx.SaveChangesAsync();

    return Results.Ok(responseText);
});

app.Run();

