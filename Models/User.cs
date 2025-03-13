using System;

namespace StudentInformationSystem.Models
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Student"; // Default role is Student
        public DateTime Created { get; set; } = DateTime.Now;
    }
}