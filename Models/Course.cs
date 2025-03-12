using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StudentInformationSystem.Models;

[Table("Courses")] // Specify the table name as 'Courses'
public class Course : BaseModel
{
    [PrimaryKey("id")]
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