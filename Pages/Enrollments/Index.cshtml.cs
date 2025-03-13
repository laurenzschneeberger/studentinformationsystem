using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudentInformationSystem.Models;
using StudentInformationSystem.Services;
using System.Text;

namespace StudentInformationSystem.Pages.Enrollments;

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
    public List<Course> AvailableCourses { get; set; } = new();
    public List<EnrolledCourseDto> EnrolledCourses { get; set; } = new();
    public SelectList StudentSelectList { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SelectedStudentId { get; set; }

    [BindProperty]
    public int CourseToAddId { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            await LoadStudentsAsync();

            if (SelectedStudentId.HasValue)
            {
                await LoadEnrolledCoursesAsync(SelectedStudentId.Value);
                await LoadAvailableCoursesAsync(SelectedStudentId.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnGetAsync");
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostAddEnrollmentAsync()
    {
        if (!SelectedStudentId.HasValue)
        {
            StatusMessage = "Error: No student selected.";
            return RedirectToPage();
        }

        try
        {
            var enrollment = await _supabaseService.AddEnrollmentAsync(SelectedStudentId.Value, CourseToAddId);
            if (enrollment != null)
            {
                StatusMessage = "Enrollment added successfully.";
            }
            else
            {
                StatusMessage = "Error: Failed to add enrollment.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding enrollment");
            StatusMessage = $"Error: {ex.Message}";
        }

        return RedirectToPage(new { SelectedStudentId });
    }

    public async Task<IActionResult> OnPostRemoveEnrollmentAsync(int studentId, int courseId)
    {
        try
        {
            var success = await _supabaseService.DeleteEnrollmentByStudentAndCourseAsync(studentId, courseId);
            if (success)
            {
                StatusMessage = "Enrollment removed successfully.";
            }
            else
            {
                StatusMessage = "Error: Failed to remove enrollment.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing enrollment");
            StatusMessage = $"Error: {ex.Message}";
        }

        return RedirectToPage(new { SelectedStudentId = studentId });
    }

    private async Task LoadStudentsAsync()
    {
        Students = await _supabaseService.GetStudentsAsync();

        // Create a list of SelectListItems with combined FirstName and LastName
        var selectItems = Students.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.FirstName} {s.LastName}",
            Selected = SelectedStudentId.HasValue && s.Id == SelectedStudentId.Value
        }).ToList();

        StudentSelectList = new SelectList(selectItems, "Value", "Text");
    }

    private async Task LoadEnrolledCoursesAsync(int studentId)
    {
        var enrollments = await _supabaseService.GetEnrollmentsForStudentAsync(studentId);
        EnrolledCourses = new List<EnrolledCourseDto>();

        foreach (var enrollment in enrollments)
        {
            var course = await _supabaseService.GetCourseByIdAsync(enrollment.CourseId);
            if (course != null)
            {
                EnrolledCourses.Add(new EnrolledCourseDto
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    CourseId = course.CourseId,
                    StudentId = studentId,
                    CourseName = course.CourseName,
                    ECTS = course.ECTS,
                    Hours = course.Hours,
                    Instructor = course.Instructor,
                    EnrollmentDate = enrollment.EnrollmentDate
                });
            }
        }
    }

    private async Task LoadAvailableCoursesAsync(int studentId)
    {
        var allCourses = await _supabaseService.GetCoursesAsync();
        var enrolledCourseIds = EnrolledCourses.Select(ec => ec.CourseId).ToList();

        // Filter out courses the student is already enrolled in
        AvailableCourses = allCourses.Where(c => !enrolledCourseIds.Contains(c.CourseId)).ToList();
    }
}