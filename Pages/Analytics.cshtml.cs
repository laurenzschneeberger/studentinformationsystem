using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentInformationSystem.Models;
using StudentInformationSystem.Services;
using System.Text.Json;

namespace StudentInformationSystem.Pages
{
    [Authorize(Roles = "Admin")]
    public class AnalyticsModel : PageModel
    {
        private readonly SupabaseService _supabaseService;

        public AnalyticsModel(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
            EnrollmentData = "[]";
            CourseFormatData = "[]";
            CourseEnrollmentData = "[]";
        }

        public string EnrollmentData { get; set; }
        public string CourseFormatData { get; set; }
        public string CourseEnrollmentData { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Get all students ordered by enrollment date
                var students = await _supabaseService.GetStudentsAsync();
                var orderedStudents = students.OrderBy(s => s.EnrollmentDate).ToList();

                var cumulativeData = new List<object>();
                int cumulative = 0;
                foreach (var student in orderedStudents)
                {
                    cumulative++;
                    cumulativeData.Add(new
                    {
                        x = student.EnrollmentDate.ToString("yyyy-MM-dd"),
                        y = cumulative
                    });
                }

                // Get all courses for format distribution
                var courses = await _supabaseService.GetCoursesAsync();
                var courseFormats = courses
                    .GroupBy(c => c.Format)
                    .Select(g => new
                    {
                        format = string.IsNullOrEmpty(g.Key) ? "Unknown" : g.Key,
                        count = g.Count()
                    })
                    .ToList();

                // Get enrollments summary from the view
                var enrollmentsSummary = await _supabaseService.GetEnrollmentsSummaryAsync();
                var courseEnrollmentData = enrollmentsSummary
                    .OrderByDescending(x => x.Enrollments)
                    .Select(x => new
                    {
                        courseName = x.CourseName,
                        studentCount = x.Enrollments
                    })
                    .ToList();

                EnrollmentData = JsonSerializer.Serialize(cumulativeData);
                CourseFormatData = JsonSerializer.Serialize(courseFormats);
                CourseEnrollmentData = JsonSerializer.Serialize(courseEnrollmentData);
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - we'll show empty charts instead
                Console.Error.WriteLine($"Error loading analytics data: {ex.Message}");
                EnrollmentData = "[]";
                CourseFormatData = "[]";
                CourseEnrollmentData = "[]";
            }
        }
    }
}