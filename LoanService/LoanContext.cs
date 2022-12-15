using Microsoft.EntityFrameworkCore;
using LoanService.Model;

namespace LoanService
{
    public class LoanContext : DbContext
    {
        public LoanContext(DbContextOptions<LoanContext> options) : base(options) { }
        public DbSet<Loan> Loans => Set<Loan>();

    }
}