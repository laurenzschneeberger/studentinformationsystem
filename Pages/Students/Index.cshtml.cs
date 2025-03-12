using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentInformationSystem.Models;
using StudentInformationSystem.Services;

namespace StudentInformationSystem.Pages.Students;

public class IndexModel : PageModel
{
    private readonly SupabaseService _supabaseService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(SupabaseService supabaseService, ILogger<IndexModel> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    public List<Student> Students { get; set; } = new();

    [BindProperty]
    public Student NewStudent { get; set; } = new Student();

    [BindProperty]
    public Student? EditingStudent { get; set; }

    // Error message for display in the view
    public string? ErrorMessage { get; set; }

    // For diagnostic results
    public string? DiagnosticResults { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Attempting to fetch all students from the database");
            Students = await _supabaseService.GetStudentsAsync();
            _logger.LogInformation("Successfully retrieved {Count} students", Students.Count);

            if (Students.Count == 0)
            {
                _logger.LogWarning("No students found in the database");
                ErrorMessage = "No students found in the database. The table might be empty or there may be a connection issue.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch students");
            ErrorMessage = $"Error retrieving students: {ex.Message}";
            Students = new List<Student>();
        }
    }

    public async Task<IActionResult> OnGetTestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Running Supabase connection diagnostic test");
            DiagnosticResults = await _supabaseService.TestConnectionAsync();
            _logger.LogInformation("Connection test completed");

            // Also attempt to load students while we're here
            Students = await _supabaseService.GetStudentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during connection test");
            DiagnosticResults = $"Error running test: {ex.Message}\n\nStack trace: {ex.StackTrace}";
            Students = new List<Student>();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                Students = await _supabaseService.GetStudentsAsync();
                return Page();
            }

            _logger.LogInformation("Adding new student: {FirstName} {LastName}", NewStudent.FirstName, NewStudent.LastName);
            var createdStudent = await _supabaseService.CreateStudentAsync(NewStudent);

            if (createdStudent == null)
            {
                StatusMessage = "Error: Failed to create student.";
                _logger.LogWarning("Failed to create student");
            }
            else
            {
                StatusMessage = $"Student {createdStudent.FirstName} {createdStudent.LastName} created successfully.";
                _logger.LogInformation("Student created with ID: {Id}", createdStudent.Id);
                NewStudent = new Student(); // Reset the form
            }

            // Refresh the student list
            Students = await _supabaseService.GetStudentsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            StatusMessage = $"Error: {ex.Message}";
            Students = await _supabaseService.GetStudentsAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        try
        {
            if (!ModelState.IsValid || EditingStudent == null)
            {
                Students = await _supabaseService.GetStudentsAsync();
                return Page();
            }

            _logger.LogInformation("Updating student with ID: {Id}", EditingStudent.Id);
            var updatedStudent = await _supabaseService.UpdateStudentAsync(EditingStudent);

            if (updatedStudent == null)
            {
                StatusMessage = "Error: Failed to update student.";
                _logger.LogWarning("Failed to update student with ID: {Id}", EditingStudent.Id);
            }
            else
            {
                StatusMessage = $"Student {updatedStudent.FirstName} {updatedStudent.LastName} updated successfully.";
                _logger.LogInformation("Student updated with ID: {Id}", updatedStudent.Id);
                EditingStudent = null; // Clear the editing state
            }

            // Refresh the student list
            Students = await _supabaseService.GetStudentsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student");
            StatusMessage = $"Error: {ex.Message}";
            Students = await _supabaseService.GetStudentsAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        try
        {
            if (EditingStudent == null || EditingStudent.Id == 0)
            {
                StatusMessage = "Error: No student selected for deletion.";
                Students = await _supabaseService.GetStudentsAsync();
                return Page();
            }

            _logger.LogInformation("Deleting student with ID: {Id}", EditingStudent.Id);
            var result = await _supabaseService.DeleteStudentAsync(EditingStudent.Id);

            if (result)
            {
                StatusMessage = "Student deleted successfully.";
                _logger.LogInformation("Student deleted with ID: {Id}", EditingStudent.Id);
                EditingStudent = null; // Clear the editing state
            }
            else
            {
                StatusMessage = "Error: Failed to delete student.";
                _logger.LogWarning("Failed to delete student with ID: {Id}", EditingStudent.Id);
            }

            // Refresh the student list
            Students = await _supabaseService.GetStudentsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student");
            StatusMessage = $"Error: {ex.Message}";
            Students = await _supabaseService.GetStudentsAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSelectAsync(int id)
    {
        try
        {
            _logger.LogInformation("Selecting student with ID: {Id}", id);
            EditingStudent = await _supabaseService.GetStudentByIdAsync(id);

            if (EditingStudent == null)
            {
                StatusMessage = "Error: Student not found.";
                _logger.LogWarning("Student with ID {Id} not found for selection", id);
            }
            else
            {
                _logger.LogInformation("Selected student with ID: {Id}", id);
            }

            // Refresh the student list
            Students = await _supabaseService.GetStudentsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting student");
            StatusMessage = $"Error: {ex.Message}";
            Students = await _supabaseService.GetStudentsAsync();
            return Page();
        }
    }

    public IActionResult OnPostClear()
    {
        EditingStudent = null;
        return RedirectToPage();
    }
}