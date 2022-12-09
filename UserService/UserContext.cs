using UserService.Model;
using Microsoft.EntityFrameworkCore;

namespace UserService
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
    }
}