using System.ComponentModel.DataAnnotations;

namespace StudentInformationSystem.Models;

public class Course
{
    [Key]
    public int CourseId { get; set; }

    [Required]
    [Display(Name = "Course Name")]
    public string CourseName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ECTS")]
    public int ECTS { get; set; }

    [Required]
    [Display(Name = "Hours")]
    public int Hours { get; set; }

    [Required]
    [Display(Name = "Format")]
    public string Format { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Instructor")]
    public string Instructor { get; set; } = string.Empty;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}