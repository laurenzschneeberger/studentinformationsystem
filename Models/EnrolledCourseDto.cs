using System.ComponentModel.DataAnnotations;

namespace StudentInformationSystem.Models;

public class EnrolledCourseDto
{
    public int EnrollmentId { get; set; }
    public int CourseId { get; set; }
    public int StudentId { get; set; }

    [Display(Name = "Course Name")]
    public string CourseName { get; set; } = string.Empty;

    [Display(Name = "ECTS")]
    public int ECTS { get; set; }

    [Display(Name = "Hours")]
    public int Hours { get; set; }

    [Display(Name = "Instructor")]
    public string Instructor { get; set; } = string.Empty;

    [Display(Name = "Enrollment Date")]
    [DataType(DataType.Date)]
    public DateTime EnrollmentDate { get; set; }
}