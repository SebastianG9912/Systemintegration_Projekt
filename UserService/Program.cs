using Microsoft.EntityFrameworkCore;
using UserService;
using UserService.Model;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);



var app = builder.Build();

app.MapPost("/register", async (User user, UserContext ctx) =>
{

});

app.Run();

