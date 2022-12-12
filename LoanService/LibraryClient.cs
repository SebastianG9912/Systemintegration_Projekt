using LoanService.Model;

namespace LoanService
{
    class LibraryClient
    {
        private HttpClient _httpClient;

        public LibraryClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Book> GetBookAsync(string bookId)
        {
            return await _httpClient.GetFromJsonAsync<Book>($"/book/{bookId}");
        }
    }
}