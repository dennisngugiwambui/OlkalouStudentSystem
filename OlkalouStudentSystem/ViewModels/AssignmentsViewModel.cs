// ===============================
// ViewModels/AssignmentsViewModel.cs
// ===============================
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace OlkalouStudentSystem.ViewModels
{
    public class AssignmentsViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private readonly FileService _fileService;
        private ObservableCollection<Assignment> _assignments;
        private Assignment _selectedAssignment;
        private ObservableCollection<Assignment> _pendingAssignments;
        private ObservableCollection<Assignment> _completedAssignments;
        private string _filterStatus = "All";

        public AssignmentsViewModel(ApiService apiService, AuthService authService, FileService fileService)
        {
            _apiService = apiService;
            _authService = authService;
            _fileService = fileService;
            Title = "Assignments";

            Assignments = new ObservableCollection<Assignment>();
            PendingAssignments = new ObservableCollection<Assignment>();
            CompletedAssignments = new ObservableCollection<Assignment>();

            LoadAssignmentsCommand = new Command(async () => await LoadAssignmentsAsync());
            SubmitAssignmentCommand = new Command<Assignment>(async (assignment) => await SubmitAssignmentAsync(assignment));
            DownloadAssignmentCommand = new Command<Assignment>(async (assignment) => await DownloadAssignmentAsync(assignment));
            ViewAssignmentCommand = new Command<Assignment>(async (assignment) => await ViewAssignmentAsync(assignment));
            FilterAssignmentsCommand = new Command<string>(FilterAssignments);

            // Initialize with dummy data
            InitializeDummyData();
        }

        public ObservableCollection<Assignment> Assignments
        {
            get => _assignments;
            set => SetProperty(ref _assignments, value);
        }

        public ObservableCollection<Assignment> PendingAssignments
        {
            get => _pendingAssignments;
            set => SetProperty(ref _pendingAssignments, value);
        }

        public ObservableCollection<Assignment> CompletedAssignments
        {
            get => _completedAssignments;
            set => SetProperty(ref _completedAssignments, value);
        }

        public Assignment SelectedAssignment
        {
            get => _selectedAssignment;
            set => SetProperty(ref _selectedAssignment, value);
        }

        public string FilterStatus
        {
            get => _filterStatus;
            set => SetProperty(ref _filterStatus, value);
        }

        public ICommand LoadAssignmentsCommand { get; }
        public ICommand SubmitAssignmentCommand { get; }
        public ICommand DownloadAssignmentCommand { get; }
        public ICommand ViewAssignmentCommand { get; }
        public ICommand FilterAssignmentsCommand { get; }

        public async Task InitializeAsync()
        {
            await LoadAssignmentsAsync();
        }

        private void InitializeDummyData()
        {
            var dummyAssignments = new List<Assignment>
            {
                new Assignment
                {
                    AssignmentId = "1",
                    Title = "Mathematics - Quadratic Equations",
                    Subject = "Mathematics",
                    Description = "Solve problems on quadratic equations and graph the functions. Show all working steps.",
                    DueDate = DateTime.Now.AddDays(3),
                    DateCreated = DateTime.Now.AddDays(-7),
                    Status = "Pending",
                    FilePath = "/assignments/math_quadratic.pdf",
                    MaxMarks = 50,
                    CreatedBy = "Mr. Johnson Kimani",
                    Form = "4A",
                    IsSubmitted = false
                },
                new Assignment
                {
                    AssignmentId = "2",
                    Title = "Physics - Laws of Motion",
                    Subject = "Physics",
                    Description = "Write a detailed report on Newton's laws of motion with real-life examples.",
                    DueDate = DateTime.Now.AddDays(5),
                    DateCreated = DateTime.Now.AddDays(-5),
                    Status = "Pending",
                    FilePath = "/assignments/physics_motion.pdf",
                    MaxMarks = 40,
                    CreatedBy = "Mrs. Sarah Wanjiku",
                    Form = "4A",
                    IsSubmitted = false
                },
                new Assignment
                {
                    AssignmentId = "3",
                    Title = "English - Essay Writing",
                    Subject = "English",
                    Description = "Write a 500-word essay on 'The Impact of Technology on Modern Society'.",
                    DueDate = DateTime.Now.AddDays(2),
                    DateCreated = DateTime.Now.AddDays(-3),
                    Status = "Pending",
                    FilePath = "/assignments/english_essay.pdf",
                    MaxMarks = 30,
                    CreatedBy = "Mr. Peter Mwangi",
                    Form = "4A",
                    IsSubmitted = false
                },
                new Assignment
                {
                    AssignmentId = "4",
                    Title = "Chemistry - Organic Compounds",
                    Subject = "Chemistry",
                    Description = "Complete the lab report on organic compound identification and analysis.",
                    DueDate = DateTime.Now.AddDays(-2),
                    DateCreated = DateTime.Now.AddDays(-14),
                    Status = "Submitted",
                    FilePath = "/assignments/chemistry_organic.pdf",
                    SubmissionPath = "/submissions/chemistry_organic_john_doe.pdf",
                    MaxMarks = 45,
                    ObtainedMarks = 38,
                    CreatedBy = "Dr. Mary Njeri",
                    Form = "4A",
                    IsSubmitted = true,
                    SubmissionDate = DateTime.Now.AddDays(-1),
                    TeacherComments = "Good work! Need to improve on nomenclature."
                },
                new Assignment
                {
                    AssignmentId = "5",
                    Title = "History - Colonial Kenya",
                    Subject = "History",
                    Description = "Research and write about the effects of colonialism in Kenya.",
                    DueDate = DateTime.Now.AddDays(-5),
                    DateCreated = DateTime.Now.AddDays(-21),
                    Status = "Graded",
                    FilePath = "/assignments/history_colonial.pdf",
                    SubmissionPath = "/submissions/history_colonial_john_doe.pdf",
                    MaxMarks = 35,
                    ObtainedMarks = 32,
                    CreatedBy = "Mr. David Kariuki",
                    Form = "4A",
                    IsSubmitted = true,
                    SubmissionDate = DateTime.Now.AddDays(-6),
                    TeacherComments = "Excellent research and analysis. Well done!"
                }
            };

            foreach (var assignment in dummyAssignments)
            {
                Assignments.Add(assignment);
            }

            FilterAssignments("All");
        }

        private async Task LoadAssignmentsAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                // Simulate API call
                await Task.Delay(1000);

                // In a real app, this would call the API
                // var student = _authService.CurrentStudent;
                // var assignments = await _apiService.GetAssignmentsAsync(student.Form);

                // For now, we use dummy data (already initialized)
                FilterAssignments(FilterStatus);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load assignments: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void FilterAssignments(string status)
        {
            FilterStatus = status;

            PendingAssignments.Clear();
            CompletedAssignments.Clear();

            var pendingList = Assignments.Where(a => a.Status == "Pending").ToList();
            var completedList = Assignments.Where(a => a.Status == "Submitted" || a.Status == "Graded").ToList();

            foreach (var assignment in pendingList)
            {
                PendingAssignments.Add(assignment);
            }

            foreach (var assignment in completedList)
            {
                CompletedAssignments.Add(assignment);
            }
        }

        private async Task SubmitAssignmentAsync(Assignment assignment)
        {
            try
            {
                var result = await Application.Current.MainPage.DisplayAlert(
                    "Submit Assignment",
                    "Choose submission method:",
                    "Upload File",
                    "Cancel");

                if (result)
                {
                    var fileData = await _fileService.PickFileAsync();
                    if (fileData != null)
                    {
                        // Simulate file upload
                        await Task.Delay(2000);

                        // Update assignment status
                        assignment.Status = "Submitted";
                        assignment.IsSubmitted = true;
                        assignment.SubmissionDate = DateTime.Now;
                        assignment.SubmissionPath = $"/submissions/{assignment.Subject.ToLower()}_{assignment.Title.ToLower().Replace(" ", "_")}_submission.pdf";

                        await Application.Current.MainPage.DisplayAlert("Success", "Assignment submitted successfully!", "OK");

                        // Refresh the assignments list
                        FilterAssignments(FilterStatus);
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to submit assignment: {ex.Message}", "OK");
            }
        }

        private async Task DownloadAssignmentAsync(Assignment assignment)
        {
            try
            {
                if (!string.IsNullOrEmpty(assignment.FilePath))
                {
                    // Simulate file download
                    await Task.Delay(1500);
                    await Application.Current.MainPage.DisplayAlert("Download", $"Assignment '{assignment.Title}' downloaded successfully!", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Assignment file not available for download.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to download assignment: {ex.Message}", "OK");
            }
        }

        private async Task ViewAssignmentAsync(Assignment assignment)
        {
            var details = $"Subject: {assignment.Subject}\n" +
                         $"Due Date: {assignment.DueDate:MMM dd, yyyy}\n" +
                         $"Created By: {assignment.CreatedBy}\n" +
                         $"Max Marks: {assignment.MaxMarks}\n" +
                         $"Status: {assignment.Status}\n\n" +
                         $"Description:\n{assignment.Description}";

            if (assignment.IsSubmitted && assignment.ObtainedMarks > 0)
            {
                details += $"\n\nObtained Marks: {assignment.ObtainedMarks}/{assignment.MaxMarks}";
                if (!string.IsNullOrEmpty(assignment.TeacherComments))
                {
                    details += $"\nTeacher Comments: {assignment.TeacherComments}";
                }
            }

            await Application.Current.MainPage.DisplayAlert(assignment.Title, details, "OK");
        }
    }
}