using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentInformationSystem.Models;
using StudentInformationSystem.Services;

namespace StudentInformationSystem.Pages.Students;

[Authorize(Roles = "Admin")]
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
    public Student? EditingStudent { get; set; }

    public string? ErrorMessage { get; set; }
    public string? DiagnosticResults { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all students");
            Students = await _supabaseService.GetStudentsAsync();
            if (Students.Count == 0)
            {
                ErrorMessage = "No students found in the database.";
                _logger.LogWarning("No students found.");
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
            _logger.LogInformation("Running connection diagnostic");
            DiagnosticResults = await _supabaseService.TestConnectionAsync();
            Students = await _supabaseService.GetStudentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during connection test");
            DiagnosticResults = $"Error: {ex.Message}\n\nStack trace: {ex.StackTrace}";
            Students = new List<Student>();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        try
        {
            _logger.LogInformation("Adding new student");
            var createdStudent = await _supabaseService.CreateStudentAsync(new Student
            {
                FirstName = EditingStudent?.FirstName ?? string.Empty,
                LastName = EditingStudent?.LastName ?? string.Empty,
                EnrollmentDate = EditingStudent?.EnrollmentDate ?? DateTime.Now,
                Email = EditingStudent?.Email ?? string.Empty
            });

            if (createdStudent == null)
            {
                StatusMessage = "Error: Failed to create student.";
            }
            else
            {
                StatusMessage = $"Student {createdStudent.FirstName} {createdStudent.LastName} created.";
            }

            EditingStudent = null; // Clear form
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
            if (EditingStudent == null || EditingStudent.Id == 0)
            {
                Students = await _supabaseService.GetStudentsAsync();
                return Page();
            }

            _logger.LogInformation("Updating student ID {Id}", EditingStudent.Id);

            // Ensure all properties have valid values
            EditingStudent.FirstName = EditingStudent.FirstName ?? string.Empty;
            EditingStudent.LastName = EditingStudent.LastName ?? string.Empty;
            EditingStudent.Email = EditingStudent.Email ?? string.Empty;

            var updated = await _supabaseService.UpdateStudentAsync(EditingStudent);

            if (updated == null)
            {
                StatusMessage = "Error: Failed to update student.";
            }
            else
            {
                StatusMessage = $"Student {updated.FirstName} {updated.LastName} updated.";
            }

            EditingStudent = null; // Clear after update
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
                StatusMessage = "Error: No student selected.";
                Students = await _supabaseService.GetStudentsAsync();
                return Page();
            }

            _logger.LogInformation("Deleting student ID {Id}", EditingStudent.Id);
            var result = await _supabaseService.DeleteStudentAsync(EditingStudent.Id);

            if (!result)
            {
                StatusMessage = "Error: Failed to delete student.";
            }
            else
            {
                StatusMessage = "Student deleted successfully.";
            }

            EditingStudent = null; // Clear after deletion
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
            _logger.LogInformation("Selecting student ID {Id}", id);
            var student = await _supabaseService.GetStudentByIdAsync(id);
            if (student == null)
            {
                StatusMessage = "Error: Student not found.";
            }
            EditingStudent = student;
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