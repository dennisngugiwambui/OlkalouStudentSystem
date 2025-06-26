// ===============================
// ViewModels/ActivitiesViewModel.cs
// ===============================
using System.Collections.ObjectModel;
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Services;
using System.Windows.Input;

namespace OlkalouStudentSystem.ViewModels
{
    public class ActivitiesViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private ObservableCollection<Activity> _activities;
        private ObservableCollection<Activity> _upcomingActivities;
        private ObservableCollection<Activity> _pastActivities;
        private Activity _selectedActivity;
        private string _filterType = "All";

        public ActivitiesViewModel(ApiService apiService, AuthService authService)
        {
            _apiService = apiService;
            _authService = authService;
            Title = "School Activities";

            Activities = new ObservableCollection<Activity>();
            UpcomingActivities = new ObservableCollection<Activity>();
            PastActivities = new ObservableCollection<Activity>();

            LoadActivitiesCommand = new Command(async () => await LoadActivitiesAsync());
            ViewActivityDetailsCommand = new Command<Activity>(async (activity) => await ViewActivityDetailsAsync(activity));
            JoinActivityCommand = new Command<Activity>(async (activity) => await JoinActivityAsync(activity));
            FilterActivitiesCommand = new Command<string>(FilterActivities);
            RefreshCommand = new Command(async () => await LoadActivitiesAsync());

            // Initialize with dummy data
            InitializeDummyData();
        }

        // Properties
        public ObservableCollection<Activity> Activities
        {
            get => _activities;
            set => SetProperty(ref _activities, value);
        }

        public ObservableCollection<Activity> UpcomingActivities
        {
            get => _upcomingActivities;
            set => SetProperty(ref _upcomingActivities, value);
        }

        public ObservableCollection<Activity> PastActivities
        {
            get => _pastActivities;
            set => SetProperty(ref _pastActivities, value);
        }

        public Activity SelectedActivity
        {
            get => _selectedActivity;
            set => SetProperty(ref _selectedActivity, value);
        }

        public string FilterType
        {
            get => _filterType;
            set => SetProperty(ref _filterType, value);
        }

        // Commands
        public ICommand LoadActivitiesCommand { get; }
        public ICommand ViewActivityDetailsCommand { get; }
        public ICommand JoinActivityCommand { get; }
        public ICommand FilterActivitiesCommand { get; }
        public ICommand RefreshCommand { get; }

        public async Task InitializeAsync()
        {
            await LoadActivitiesAsync();
        }

        private void InitializeDummyData()
        {
            var dummyActivities = new List<Activity>
            {
                new Activity
                {
                    ActivityId = "ACT001",
                    Title = "Inter-House Sports Day",
                    Description = "Annual sports competition between the four school houses. Events include athletics, football, volleyball, basketball, and traditional games.",
                    Date = DateTime.Now.AddDays(14),
                    StartTime = DateTime.Now.AddDays(14).Date.AddHours(8),
                    EndTime = DateTime.Now.AddDays(14).Date.AddHours(17),
                    Venue = "School Sports Complex",
                    ActivityType = "Sports",
                    Organizer = "Sports Department",
                    IsOptional = false,
                    Requirements = "Sports uniform, running shoes, water bottle",
                    TargetForms = new List<string> { "Form 1", "Form 2", "Form 3", "Form 4" },
                    Status = "Scheduled"
                },
                new Activity
                {
                    ActivityId = "ACT002",
                    Title = "Science Exhibition",
                    Description = "Students showcase their science projects and innovations. Judges from local universities will evaluate the projects.",
                    Date = DateTime.Now.AddDays(21),
                    StartTime = DateTime.Now.AddDays(21).Date.AddHours(9),
                    EndTime = DateTime.Now.AddDays(21).Date.AddHours(16),
                    Venue = "School Hall",
                    ActivityType = "Academic",
                    Organizer = "Science Department",
                    IsOptional = true,
                    Requirements = "Science project, display materials, lab coat",
                    TargetForms = new List<string> { "Form 3", "Form 4" },
                    Status = "Scheduled"
                },
                new Activity
                {
                    ActivityId = "ACT003",
                    Title = "Cultural Day Celebration",
                    Description = "Celebration of Kenyan cultural diversity with traditional dances, food, attire, and music performances.",
                    Date = DateTime.Now.AddDays(28),
                    StartTime = DateTime.Now.AddDays(28).Date.AddHours(10),
                    EndTime = DateTime.Now.AddDays(28).Date.AddHours(15),
                    Venue = "School Compound",
                    ActivityType = "Cultural",
                    Organizer = "Cultural Committee",
                    IsOptional = false,
                    Requirements = "Traditional attire (optional), participation in group activities",
                    TargetForms = new List<string> { "Form 1", "Form 2", "Form 3", "Form 4" },
                    Status = "Scheduled"
                },
                new Activity
                {
                    ActivityId = "ACT004",
                    Title = "Career Guidance Workshop",
                    Description = "Professional guidance on career choices and university applications. Guest speakers from various professions.",
                    Date = DateTime.Now.AddDays(7),
                    StartTime = DateTime.Now.AddDays(7).Date.AddHours(14),
                    EndTime = DateTime.Now.AddDays(7).Date.AddHours(16),
                    Venue = "Library Hall",
                    ActivityType = "Academic",
                    Organizer = "Guidance & Counseling Department",
                    IsOptional = false,
                    Requirements = "Notebook, pen, career interest questionnaire",
                    TargetForms = new List<string> { "Form 4" },
                    Status = "Scheduled"
                },
                new Activity
                {
                    ActivityId = "ACT005",
                    Title = "Environmental Conservation Day",
                    Description = "Tree planting and environmental awareness activities around the school compound and neighboring areas.",
                    Date = DateTime.Now.AddDays(35),
                    StartTime = DateTime.Now.AddDays(35).Date.AddHours(7),
                    EndTime = DateTime.Now.AddDays(35).Date.AddHours(12),
                    Venue = "School Compound & Surrounding Areas",
                    ActivityType = "Community Service",
                    Organizer = "Environmental Club",
                    IsOptional = true,
                    Requirements = "Old clothes, gloves, water bottle",
                    TargetForms = new List<string> { "Form 1", "Form 2", "Form 3", "Form 4" },
                    Status = "Scheduled"
                },
                new Activity
                {
                    ActivityId = "ACT006",
                    Title = "Mathematics Contest",
                    Description = "Inter-school mathematics competition with students from neighboring schools. Cash prizes for winners.",
                    Date = DateTime.Now.AddDays(-7),
                    StartTime = DateTime.Now.AddDays(-7).Date.AddHours(9),
                    EndTime = DateTime.Now.AddDays(-7).Date.AddHours(12),
                    Venue = "Mathematics Laboratory",
                    ActivityType = "Academic",
                    Organizer = "Mathematics Department",
                    IsOptional = true,
                    Requirements = "Calculator, mathematical instruments, writing materials",
                    TargetForms = new List<string> { "Form 3", "Form 4" },
                    Status = "Completed"
                },
                new Activity
                {
                    ActivityId = "ACT007",
                    Title = "Drama Festival",
                    Description = "Annual drama performances by students. Parents and community members are invited to attend.",
                    Date = DateTime.Now.AddDays(-14),
                    StartTime = DateTime.Now.AddDays(-14).Date.AddHours(18),
                    EndTime = DateTime.Now.AddDays(-14).Date.AddHours(21),
                    Venue = "School Hall",
                    ActivityType = "Cultural",
                    Organizer = "Drama Club",
                    IsOptional = true,
                    Requirements = "Costumes (provided), makeup, scripts",
                    TargetForms = new List<string> { "Form 1", "Form 2", "Form 3", "Form 4" },
                    Status = "Completed"
                }
            };

            foreach (var activity in dummyActivities)
            {
                Activities.Add(activity);
            }

            FilterActivities("All");
        }

        private async Task LoadActivitiesAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                // Simulate API call
                await Task.Delay(1000);

                // In a real app, this would call:
                // var student = _authService.CurrentStudent;
                // var activities = await _apiService.GetActivitiesAsync(student.Form);

                // For demo, we already have dummy data initialized
                FilterActivities(FilterType);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load activities: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void FilterActivities(string filterType)
        {
            FilterType = filterType;

            UpcomingActivities.Clear();
            PastActivities.Clear();

            var now = DateTime.Now;
            var upcoming = Activities.Where(a => a.Date >= now).OrderBy(a => a.Date).ToList();
            var past = Activities.Where(a => a.Date < now).OrderByDescending(a => a.Date).ToList();

            foreach (var activity in upcoming)
            {
                if (filterType == "All" || activity.ActivityType == filterType)
                {
                    UpcomingActivities.Add(activity);
                }
            }

            foreach (var activity in past)
            {
                if (filterType == "All" || activity.ActivityType == filterType)
                {
                    PastActivities.Add(activity);
                }
            }
        }

        private async Task ViewActivityDetailsAsync(Activity activity)
        {
            var targetFormsText = string.Join(", ", activity.TargetForms);

            var details = $"📅 Date: {activity.Date:MMM dd, yyyy}\n" +
                         $"⏰ Time: {activity.StartTime:HH:mm} - {activity.EndTime:HH:mm}\n" +
                         $"📍 Venue: {activity.Venue}\n" +
                         $"🏷️ Type: {activity.ActivityType}\n" +
                         $"👨‍🏫 Organizer: {activity.Organizer}\n" +
                         $"📝 Status: {activity.Status}\n" +
                         $"🎯 Target Forms: {targetFormsText}\n" +
                         $"📋 Optional: {(activity.IsOptional ? "Yes" : "No")}\n\n" +
                         $"📝 Description:\n{activity.Description}\n\n";

            if (!string.IsNullOrEmpty(activity.Requirements))
            {
                details += $"✅ Requirements:\n{activity.Requirements}";
            }

            await Application.Current.MainPage.DisplayAlert(activity.Title, details, "OK");
        }

        private async Task JoinActivityAsync(Activity activity)
        {
            try
            {
                if (activity.Date < DateTime.Now)
                {
                    await Application.Current.MainPage.DisplayAlert("Activity Past", "This activity has already taken place.", "OK");
                    return;
                }

                if (activity.Status == "Cancelled")
                {
                    await Application.Current.MainPage.DisplayAlert("Activity Cancelled", "This activity has been cancelled.", "OK");
                    return;
                }

                var message = activity.IsOptional
                    ? $"Do you want to register for '{activity.Title}'?"
                    : $"This activity is mandatory. Do you want to confirm your attendance for '{activity.Title}'?";

                var result = await Application.Current.MainPage.DisplayAlert(
                    "Join Activity",
                    message,
                    "Yes", "No");

                if (result)
                {
                    // Simulate API call
                    await Task.Delay(1500);

                    var successMessage = activity.IsOptional
                        ? "Successfully registered for the activity!"
                        : "Attendance confirmed for the mandatory activity!";

                    await Application.Current.MainPage.DisplayAlert("Success", successMessage, "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to join activity: {ex.Message}", "OK");
            }
        }
    }
}