using UserService.Model;
using Microsoft.EntityFrameworkCore;

namespace UserService
{
    public class UserContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        public UserContext(DbContextOptions<UserContext> options) : base(options) { }
    }
}