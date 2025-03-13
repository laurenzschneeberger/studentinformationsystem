using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StudentInformationSystem.Models;

/// <summary>
/// Data Transfer Object for Course operations with the database.
/// This class excludes navigation properties to prevent serialization issues.
/// </summary>
[Table("Courses")]
public class CourseDto : BaseModel
{
    [PrimaryKey("course_id")]
    public int CourseId { get; set; }

    [Column("course_name")]
    public string CourseName { get; set; } = string.Empty;

    [Column("ects")]
    public int ECTS { get; set; }

    [Column("hours")]
    public int Hours { get; set; }

    [Column("format")]
    public string Format { get; set; } = string.Empty;

    [Column("instructor")]
    public string Instructor { get; set; } = string.Empty;

    /// <summary>
    /// Convert from a Course model to a CourseDto
    /// </summary>
    public static CourseDto FromCourse(Course course)
    {
        return new CourseDto
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            ECTS = course.ECTS,
            Hours = course.Hours,
            Format = course.Format,
            Instructor = course.Instructor
        };
    }

    /// <summary>
    /// Convert to a Course model from this DTO
    /// </summary>
    public Course ToCourse()
    {
        return new Course
        {
            CourseId = this.CourseId,
            CourseName = this.CourseName,
            ECTS = this.ECTS,
            Hours = this.Hours,
            Format = this.Format,
            Instructor = this.Instructor
        };
    }
}