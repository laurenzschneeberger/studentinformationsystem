using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentInformationSystem.Models;
using StudentInformationSystem.Services;
using System.Text;

namespace StudentInformationSystem.Pages.Courses;

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

    public List<Course> Courses { get; set; } = new();

    [BindProperty]
    public Course? EditingCourse { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all courses");
            Courses = await _supabaseService.GetCoursesAsync();
            if (Courses.Count == 0)
            {
                ErrorMessage = "No courses found in the database.";
                _logger.LogWarning("No courses found.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch courses");
            ErrorMessage = $"Error retrieving courses: {ex.Message}";
            Courses = new List<Course>();
        }
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        try
        {
            _logger.LogInformation("Adding new course");

            // Specific check for CourseName
            if (string.IsNullOrWhiteSpace(EditingCourse?.CourseName))
            {
                StatusMessage = "Error: Course name is required.";
                Courses = await _supabaseService.GetCoursesAsync();
                return Page();
            }

            // Create a new course object with validated values
            var newCourse = new Course
            {
                CourseName = EditingCourse?.CourseName.Trim() ?? string.Empty,
                ECTS = EditingCourse?.ECTS ?? 0,
                Hours = EditingCourse?.Hours ?? 0,
                Format = EditingCourse?.Format ?? string.Empty,
                Instructor = EditingCourse?.Instructor ?? string.Empty
            };

            // Log the course being created
            _logger.LogInformation("Creating course with name: {CourseName}", newCourse.CourseName);

            // Use the course-specific method from SupabaseService
            var createdCourse = await _supabaseService.CreateCourseAsync(newCourse);

            if (createdCourse != null)
            {
                StatusMessage = $"Course {createdCourse.CourseName} created successfully.";
                _logger.LogInformation("Successfully created course with name: {CourseName}", createdCourse.CourseName);
            }
            else
            {
                StatusMessage = "Error: Failed to create course.";
                _logger.LogError("Failed to create course - null returned from service");
            }

            EditingCourse = null; // Clear form
            Courses = await _supabaseService.GetCoursesAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course");
            StatusMessage = $"Error: {ex.Message}";
            Courses = await _supabaseService.GetCoursesAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        try
        {
            if (EditingCourse == null || EditingCourse.CourseId == 0)
            {
                StatusMessage = "Error: No course selected for update.";
                Courses = await _supabaseService.GetCoursesAsync();
                return Page();
            }

            // Specific check for CourseName
            if (string.IsNullOrWhiteSpace(EditingCourse.CourseName))
            {
                StatusMessage = "Error: Course name is required.";
                Courses = await _supabaseService.GetCoursesAsync();
                return Page();
            }

            _logger.LogInformation("Updating course ID {Id} with name {Name}",
                EditingCourse.CourseId, EditingCourse.CourseName);

            // Ensure all properties have valid values
            EditingCourse.CourseName = EditingCourse.CourseName.Trim();
            EditingCourse.Format = EditingCourse.Format ?? string.Empty;
            EditingCourse.Instructor = EditingCourse.Instructor ?? string.Empty;

            // Use the course-specific method from SupabaseService
            var updatedCourse = await _supabaseService.UpdateCourseAsync(EditingCourse);

            if (updatedCourse != null)
            {
                StatusMessage = $"Course {updatedCourse.CourseName} updated successfully.";
                _logger.LogInformation("Successfully updated course with name: {CourseName}", updatedCourse.CourseName);
            }
            else
            {
                StatusMessage = "Error: Failed to update course.";
                _logger.LogError("Failed to update course - null returned from service");
            }

            EditingCourse = null; // Clear after update
            Courses = await _supabaseService.GetCoursesAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course");
            StatusMessage = $"Error: {ex.Message}";
            Courses = await _supabaseService.GetCoursesAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        try
        {
            if (EditingCourse == null || EditingCourse.CourseId == 0)
            {
                StatusMessage = "Error: No course selected.";
                Courses = await _supabaseService.GetCoursesAsync();
                return Page();
            }

            _logger.LogInformation("Deleting course ID {Id}", EditingCourse.CourseId);

            // Use the course-specific method from SupabaseService
            var result = await _supabaseService.DeleteCourseAsync(EditingCourse.CourseId);

            if (result)
            {
                StatusMessage = "Course deleted successfully.";
            }
            else
            {
                StatusMessage = "Error: Failed to delete course.";
            }

            EditingCourse = null; // Clear after deletion
            Courses = await _supabaseService.GetCoursesAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course");
            StatusMessage = $"Error: {ex.Message}";
            Courses = await _supabaseService.GetCoursesAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSelectAsync(int id)
    {
        try
        {
            _logger.LogInformation("Selecting course ID {Id}", id);

            // Use the course-specific method from SupabaseService
            var course = await _supabaseService.GetCourseByIdAsync(id);

            if (course != null)
            {
                EditingCourse = course;
            }
            else
            {
                StatusMessage = "Error: Course not found.";
            }

            Courses = await _supabaseService.GetCoursesAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting course");
            StatusMessage = $"Error: {ex.Message}";
            Courses = await _supabaseService.GetCoursesAsync();
            return Page();
        }
    }

    public IActionResult OnPostClear()
    {
        EditingCourse = null;
        return RedirectToPage();
    }
}