namespace LoanService.Model
{
    public class Book
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public List<Loan> Loans { get; set; } = new List<Loan>();
    }
}
