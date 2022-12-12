using Microsoft.EntityFrameworkCore;
using LibraryService.Model;

namespace LibraryService
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }
        public DbSet<Book> Books => Set<Book>();

    }
}