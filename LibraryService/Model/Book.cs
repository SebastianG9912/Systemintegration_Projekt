using LoanService.Model;


namespace LibraryService.Model
{
    public class Book
    {
        public Guid Id { get; set; }
        public string Title { get; set; }

        public List<Loan> Loans { get; set; } = new List<Loan>();
    }
}