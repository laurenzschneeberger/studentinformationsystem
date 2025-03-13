using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StudentInformationSystem.Models;

[Table("EnrollmentsSummary")]
public class EnrollmentSummary : BaseModel
{
    [Column("course_name")]
    public string CourseName { get; set; } = string.Empty;

    [Column("enrollments")]
    public int Enrollments { get; set; }
}