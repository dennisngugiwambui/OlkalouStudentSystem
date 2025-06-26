using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using OlkalouStudentSystem.Models;

namespace OlkalouStudentSystem.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private Student _student;
        private DashboardData _dashboardData;
        private string _title = "Dashboard";

        public DashboardViewModel()
        {
            LoadDashboardCommand = new Command(async () => await LoadDashboardAsync());
            ViewFeesCommand = new Command(async () => await ViewFeesAsync());
            ViewAssignmentsCommand = new Command(async () => await ViewAssignmentsAsync());
            ViewLibraryCommand = new Command(async () => await ViewLibraryAsync());
            ViewActivitiesCommand = new Command(async () => await ViewActivitiesAsync());
            ViewPerformanceCommand = new Command(async () => await ViewPerformanceAsync());
            ViewSchoolInfoCommand = new Command(async () => await ViewSchoolInfoAsync());
            LogoutCommand = new Command(async () => await LogoutAsync());

            // Initialize with dummy data
            InitializeDummyData();
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public Student Student
        {
            get => _student;
            set
            {
                _student = value;
                OnPropertyChanged();
            }
        }

        public DashboardData DashboardData
        {
            get => _dashboardData;
            set
            {
                _dashboardData = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand LoadDashboardCommand { get; }
        public ICommand ViewFeesCommand { get; }
        public ICommand ViewAssignmentsCommand { get; }
        public ICommand ViewLibraryCommand { get; }
        public ICommand ViewActivitiesCommand { get; }
        public ICommand ViewPerformanceCommand { get; }
        public ICommand ViewSchoolInfoCommand { get; }
        public ICommand ViewAllActivitiesCommand { get; }
        public ICommand LogoutCommand { get; }

        public async Task InitializeAsync()
        {
            await LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // Simulate API call
                await Task.Delay(1000);

                // Refresh data
                InitializeDummyData();
            }
            catch (Exception ex)
            {
                // Handle error
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to load dashboard data", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void InitializeDummyData()
        {
            Student = new Student
            {
                FullName = "John Doe",
                AdmissionNo = "GRS/2023/001",
                Form = "4A",
                Class = "Form 4",
                Email = "john.doe@graceschool.ac.ke"
            };

            DashboardData = new DashboardData
            {
                CurrentFees = new FeesInfo
                {
                    StudentId = "GRS_2023_001",
                    TotalFees = 80000,
                    PaidAmount = 55000,
                    Balance = 25000, // Now we can set this directly
                    LastPaymentDate = DateTime.Now.AddDays(-30),
                    PaymentStatus = "Pending",
                    DueDate = DateTime.Now.AddDays(15)
                },
                PendingAssignments = new ObservableCollection<Assignment>
                {
                    new Assignment { Title = "Mathematics Assignment", DueDate = DateTime.Now.AddDays(3), Subject = "Mathematics" },
                    new Assignment { Title = "Physics Lab Report", DueDate = DateTime.Now.AddDays(5), Subject = "Physics" },
                    new Assignment { Title = "English Essay", DueDate = DateTime.Now.AddDays(2), Subject = "English" },
                    new Assignment { Title = "Chemistry Project", DueDate = DateTime.Now.AddDays(7), Subject = "Chemistry" }
                },
                IssuedBooks = new ObservableCollection<LibraryBook>
                {
                    new LibraryBook { Title = "Advanced Mathematics", Author = "John Smith", DueDate = DateTime.Now.AddDays(14) },
                    new LibraryBook { Title = "Physics Principles", Author = "Jane Doe", DueDate = DateTime.Now.AddDays(10) },
                    new LibraryBook { Title = "English Literature", Author = "Mark Johnson", DueDate = DateTime.Now.AddDays(12) }
                },
                RecentAchievements = new ObservableCollection<Achievement>
                {
                    new Achievement { Title = "Mathematics Excellence", Date = DateTime.Now.AddDays(-5), Type = "Academic" },
                    new Achievement { Title = "Best Student Award", Date = DateTime.Now.AddDays(-10), Type = "General" },
                    new Achievement { Title = "Science Fair Winner", Date = DateTime.Now.AddDays(-15), Type = "Competition" }
                },
                UpcomingActivities = new ObservableCollection<Activity>
                {
                    new Activity
                    {
                        Title = "Parent-Teacher Meeting",
                        Description = "Quarterly academic progress discussion with parents and teachers",
                        Date = DateTime.Now.AddDays(7)
                    },
                    new Activity
                    {
                        Title = "Inter-House Sports Day",
                        Description = "Annual sports competition between school houses",
                        Date = DateTime.Now.AddDays(14)
                    },
                    new Activity
                    {
                        Title = "Science Exhibition",
                        Description = "Student science projects showcase and competition",
                        Date = DateTime.Now.AddDays(21)
                    },
                    new Activity
                    {
                        Title = "End of Term Exams",
                        Description = "Comprehensive examinations for all subjects",
                        Date = DateTime.Now.AddDays(30)
                    }
                },
                AcademicPerformance = new ObservableCollection<SubjectPerformance>
                {
                    new SubjectPerformance { Subject = "Mathematics", CurrentGrade = 85, PreviousGrade = 78, Trend = "up" },
                    new SubjectPerformance { Subject = "English", CurrentGrade = 92, PreviousGrade = 89, Trend = "up" },
                    new SubjectPerformance { Subject = "Science", CurrentGrade = 78, PreviousGrade = 82, Trend = "down" },
                    new SubjectPerformance { Subject = "History", CurrentGrade = 89, PreviousGrade = 85, Trend = "up" },
                    new SubjectPerformance { Subject = "Geography", CurrentGrade = 83, PreviousGrade = 80, Trend = "up" }
                }
            };
        }

        private async Task ViewFeesAsync()
        {
            await Shell.Current.GoToAsync("//main/fees");
        }

        private async Task ViewAssignmentsAsync()
        {
            await Shell.Current.GoToAsync("//main/assignments");
        }

        private async Task ViewLibraryAsync()
        {
            await Shell.Current.GoToAsync("//main/library");
        }

        private async Task ViewActivitiesAsync()
        {
            await Shell.Current.GoToAsync("//main/activities");
        }

        private async Task ViewPerformanceAsync()
        {
            await Application.Current.MainPage.DisplayAlert("Performance", "Detailed performance view coming soon!", "OK");
        }

        private async Task ViewSchoolInfoAsync()
        {
            await Application.Current.MainPage.DisplayAlert(
                "Grace Secondary School",
                "📍 Location: Olkalou, Nyandarua County\n" +
                "📞 Phone: +254 724 437 239\n" +
                "📧 Email: info@graceschool.ac.ke\n" +
                "🎓 Established: 1998\n" +
                "👥 Students: 850+\n" +
                "👨‍🏫 Teachers: 45\n\n" +
                "Grace Secondary School is committed to providing quality education through prayers, discipline, and hard work.",
                "Close");
        }

        private async Task ViewAllActivitiesAsync()
        {
            await Shell.Current.GoToAsync("//main/activities");
        }

        private async Task LogoutAsync()
        {
            var result = await Application.Current.MainPage.DisplayAlert(
                "Logout",
                "Are you sure you want to logout?",
                "Yes",
                "No");

            if (result)
            {
                // Clear any stored credentials/tokens here
                await Shell.Current.GoToAsync("//login");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}