using Grpc.Core;
using LibraryService.Protos;

namespace LibraryService.Services
{
    public class BookService : GetBookService.GetBookServiceBase
    {
        private readonly ILogger<BookService> _logger;
        private readonly LibraryContext _ctx;

        public BookService(ILogger<BookService> logger, LibraryContext ctx)
        {
            _logger = logger;
            _ctx = ctx;
        }

        public override async Task<BookResponse> GetBook(BookRequest request, ServerCallContext serverCallContext)
        {
            var idGuid = new Guid(request.BookId);
            var book = await _ctx.Books.FindAsync(idGuid);

            var response = new BookResponse();

            if (book != null)
            {
                response.Id = book.Id.ToString();
                response.Title = book.Title;
                response.Loaned = book.Loaned;
            }

            return await Task.FromResult(response);
        }
    }
}