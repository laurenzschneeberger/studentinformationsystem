using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StudentInformationSystem.Models;

[Table("Enrollments")]
public class Enrollment : BaseModel
{
    [PrimaryKey("enrollment_id")]
    public int EnrollmentId { get; set; }

    [Column("student_id")]
    [Required]
    public int StudentId { get; set; }

    [Column("course_id")]
    [Required]
    public int CourseId { get; set; }

    [Column("enrollment_date")]
    [Required]
    [Display(Name = "Enrollment Date")]
    [DataType(DataType.Date)]
    public DateTime EnrollmentDate { get; set; }
}