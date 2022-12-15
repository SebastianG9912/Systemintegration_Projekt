namespace LibraryService.Model
{
    public class Book
    {
        public Guid Id { get; set; }
        public string Title { get; set; }

        public bool Loaned { get; set; }
    }
}