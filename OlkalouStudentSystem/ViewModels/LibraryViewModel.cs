// ===============================
// ViewModels/LibraryViewModel.cs
// ===============================
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace OlkalouStudentSystem.ViewModels
{
    public class LibraryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private ObservableCollection<BookIssue> _issuedBooks;
        private ObservableCollection<LibraryBook> _availableBooks;
        private ObservableCollection<LibraryBook> _searchResults;
        private string _searchQuery;
        private bool _isSearching;
        private BookIssue _selectedIssuedBook;
        private LibraryBook _selectedAvailableBook;

        public LibraryViewModel(ApiService apiService, AuthService authService)
        {
            _apiService = apiService;
            _authService = authService;
            Title = "Library";

            IssuedBooks = new ObservableCollection<BookIssue>();
            AvailableBooks = new ObservableCollection<LibraryBook>();
            SearchResults = new ObservableCollection<LibraryBook>();

            LoadIssuedBooksCommand = new Command(async () => await LoadIssuedBooksAsync());
            SearchBooksCommand = new Command(async () => await SearchBooksAsync());
            ClearSearchCommand = new Command(ClearSearch);
            ViewBookDetailsCommand = new Command<LibraryBook>(async (book) => await ViewBookDetailsAsync(book));
            RenewBookCommand = new Command<BookIssue>(async (bookIssue) => await RenewBookAsync(bookIssue));
            RequestBookCommand = new Command<LibraryBook>(async (book) => await RequestBookAsync(book));

            // Initialize with dummy data
            InitializeDummyData();
        }

        // Properties
        public ObservableCollection<BookIssue> IssuedBooks
        {
            get => _issuedBooks;
            set => SetProperty(ref _issuedBooks, value);
        }

        public ObservableCollection<LibraryBook> AvailableBooks
        {
            get => _availableBooks;
            set => SetProperty(ref _availableBooks, value);
        }

        public ObservableCollection<LibraryBook> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public bool IsSearching
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        public BookIssue SelectedIssuedBook
        {
            get => _selectedIssuedBook;
            set => SetProperty(ref _selectedIssuedBook, value);
        }

        public LibraryBook SelectedAvailableBook
        {
            get => _selectedAvailableBook;
            set => SetProperty(ref _selectedAvailableBook, value);
        }

        // Commands
        public ICommand LoadIssuedBooksCommand { get; }
        public ICommand SearchBooksCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ViewBookDetailsCommand { get; }
        public ICommand RenewBookCommand { get; }
        public ICommand RequestBookCommand { get; }

        public async Task InitializeAsync()
        {
            await LoadIssuedBooksAsync();
            LoadAvailableBooks();
        }

        private void InitializeDummyData()
        {
            // Dummy issued books
            var dummyIssuedBooks = new List<BookIssue>
            {
                new BookIssue
                {
                    IssueId = "I001",
                    StudentId = "GRS_2023_001",
                    StudentName = "John Doe",
                    BookId = "B001",
                    BookTitle = "Advanced Mathematics Form 4",
                    Author = "Dr. James Wachira",
                    IssueDate = DateTime.Now.AddDays(-10),
                    DueDate = DateTime.Now.AddDays(4),
                    Status = "Issued"
                },
                new BookIssue
                {
                    IssueId = "I002",
                    StudentId = "GRS_2023_001",
                    StudentName = "John Doe",
                    BookId = "B002",
                    BookTitle = "Physics Principles and Practice",
                    Author = "Prof. Mary Kiprotich",
                    IssueDate = DateTime.Now.AddDays(-8),
                    DueDate = DateTime.Now.AddDays(6),
                    Status = "Issued"
                },
                new BookIssue
                {
                    IssueId = "I003",
                    StudentId = "GRS_2023_001",
                    StudentName = "John Doe",
                    BookId = "B003",
                    BookTitle = "English Literature Anthology",
                    Author = "Prof. Susan Macharia",
                    IssueDate = DateTime.Now.AddDays(-15),
                    DueDate = DateTime.Now.AddDays(-1),
                    Status = "Overdue",
                    FineAmount = 50.00m
                }
            };

            foreach (var book in dummyIssuedBooks)
            {
                IssuedBooks.Add(book);
            }

            // Dummy available books
            LoadAvailableBooks();
        }

        private void LoadAvailableBooks()
        {
            var dummyAvailableBooks = new List<LibraryBook>
            {
                new LibraryBook
                {
                    BookId = "B004",
                    Title = "Chemistry for Secondary Schools",
                    Author = "Dr. Peter Njoroge",
                    ISBN = "978-9966-25-123-4",
                    Category = "Science",
                    Publisher = "East African Publishers",
                    PublicationYear = 2022,
                    TotalCopies = 25,
                    AvailableCopies = 18,
                    Description = "Comprehensive chemistry textbook covering Form 1-4 syllabus"
                },
                new LibraryBook
                {
                    BookId = "B005",
                    Title = "History and Government of Kenya",
                    Author = "Prof. Grace Wanjiku",
                    ISBN = "978-9966-25-124-1",
                    Category = "Social Studies",
                    Publisher = "Kenya Literature Bureau",
                    PublicationYear = 2023,
                    TotalCopies = 30,
                    AvailableCopies = 22,
                    Description = "Complete guide to Kenyan history and government structures"
                },
                new LibraryBook
                {
                    BookId = "B006",
                    Title = "Biology: Life Processes",
                    Author = "Dr. Samuel Mutua",
                    ISBN = "978-9966-25-125-8",
                    Category = "Science",
                    Publisher = "Longhorn Publishers",
                    PublicationYear = 2022,
                    TotalCopies = 20,
                    AvailableCopies = 15,
                    Description = "Detailed study of biological processes and systems"
                },
                new LibraryBook
                {
                    BookId = "B007",
                    Title = "Geography of East Africa",
                    Author = "Prof. Jane Muthoni",
                    ISBN = "978-9966-25-126-5",
                    Category = "Geography",
                    Publisher = "Jomo Kenyatta Foundation",
                    PublicationYear = 2021,
                    TotalCopies = 15,
                    AvailableCopies = 0,
                    Description = "Comprehensive study of East African geography and climate"
                },
                new LibraryBook
                {
                    BookId = "B008",
                    Title = "Computer Studies for Beginners",
                    Author = "Mr. David Karanja",
                    ISBN = "978-9966-25-127-2",
                    Category = "Technology",
                    Publisher = "Phoenix Publishers",
                    PublicationYear = 2023,
                    TotalCopies = 12,
                    AvailableCopies = 8,
                    Description = "Introduction to computer programming and applications"
                }
            };

            AvailableBooks.Clear();
            foreach (var book in dummyAvailableBooks)
            {
                AvailableBooks.Add(book);
            }
        }

        private async Task LoadIssuedBooksAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                // Simulate API call
                await Task.Delay(1000);

                // In a real app, this would call:
                // var student = _authService.CurrentStudent;
                // var books = await _apiService.GetIssuedBooksAsync(student.StudentId);

                // For demo, we already have dummy data initialized
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load issued books: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchBooksAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                SearchResults.Clear();
                return;
            }

            IsSearching = true;

            try
            {
                // Simulate API call
                await Task.Delay(800);

                // Filter available books based on search query
                var filteredBooks = AvailableBooks.Where(book =>
                    book.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    book.Author.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    book.Category.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                SearchResults.Clear();
                foreach (var book in filteredBooks)
                {
                    SearchResults.Add(book);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Search failed: {ex.Message}", "OK");
            }
            finally
            {
                IsSearching = false;
            }
        }

        private void ClearSearch()
        {
            SearchQuery = "";
            SearchResults.Clear();
        }

        private async Task ViewBookDetailsAsync(LibraryBook book)
        {
            var details = $"Title: {book.Title}\n" +
                         $"Author: {book.Author}\n" +
                         $"Category: {book.Category}\n" +
                         $"Publisher: {book.Publisher}\n" +
                         $"Publication Year: {book.PublicationYear}\n" +
                         $"ISBN: {book.ISBN}\n" +
                         $"Available Copies: {book.AvailableCopies}/{book.TotalCopies}\n\n" +
                         $"Description:\n{book.Description}";

            await Application.Current.MainPage.DisplayAlert(book.Title, details, "OK");
        }

        private async Task RenewBookAsync(BookIssue bookIssue)
        {
            try
            {
                var result = await Application.Current.MainPage.DisplayAlert(
                    "Renew Book",
                    $"Do you want to renew '{bookIssue.BookTitle}'?",
                    "Yes", "No");

                if (result)
                {
                    // Simulate API call
                    await Task.Delay(1000);

                    // Update due date
                    bookIssue.DueDate = DateTime.Now.AddDays(14);
                    bookIssue.Status = "Issued";
                    bookIssue.FineAmount = null;

                    await Application.Current.MainPage.DisplayAlert("Success", "Book renewed successfully! New due date: " + bookIssue.DueDate.ToString("MMM dd, yyyy"), "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to renew book: {ex.Message}", "OK");
            }
        }

        private async Task RequestBookAsync(LibraryBook book)
        {
            try
            {
                if (book.AvailableCopies <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Unavailable", "This book is currently not available. You can add it to your wish list.", "OK");
                    return;
                }

                var result = await Application.Current.MainPage.DisplayAlert(
                    "Request Book",
                    $"Do you want to request '{book.Title}'?",
                    "Yes", "No");

                if (result)
                {
                    // Simulate API call
                    await Task.Delay(1500);

                    await Application.Current.MainPage.DisplayAlert("Success", "Book request submitted successfully! You can collect it from the library.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to request book: {ex.Message}", "OK");
            }
        }
    }
}