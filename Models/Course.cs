using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

namespace StudentInformationSystem.Models;

[Table("Courses")] // Specify the table name as 'Courses'
public class Course : BaseModel
{
    [PrimaryKey("course_id")]
    public int CourseId { get; set; }

    [Required]
    [Display(Name = "Course Name")]
    [Column("course_name")]
    public string CourseName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ECTS")]
    [Column("ects")]
    public int ECTS { get; set; }

    [Required]
    [Display(Name = "Hours")]
    [Column("hours")]
    public int Hours { get; set; }

    [Required]
    [Display(Name = "Format")]
    [Column("format")]
    public string Format { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Instructor")]
    [Column("instructor")]
    public string Instructor { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}