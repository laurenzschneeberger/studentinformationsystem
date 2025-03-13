using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using StudentInformationSystem.Models;

namespace StudentInformationSystem.Services
{
    public class UserService
    {
        private readonly string _userFilePath;

        public UserService(IWebHostEnvironment environment)
        {
            // Store the file in the app data directory
            _userFilePath = Path.Combine(environment.ContentRootPath, "user_accounts.json");

            // Create the file if it doesn't exist
            if (!File.Exists(_userFilePath))
            {
                File.WriteAllText(_userFilePath, "[]");
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                if (!File.Exists(_userFilePath))
                {
                    return new List<User>();
                }

                var json = await File.ReadAllTextAsync(_userFilePath);
                var users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
                return users;
            }
            catch (Exception)
            {
                // Return empty list if there's an error reading the file
                return new List<User>();
            }
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            try
            {
                // Get existing users
                var users = await GetAllUsersAsync();

                // Check if username already exists
                if (users.Any(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                // Add new user
                users.Add(user);

                // Save back to file
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(users, options);
                await File.WriteAllTextAsync(_userFilePath, json);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            try
            {
                var users = await GetAllUsersAsync();
                return users.FirstOrDefault(u =>
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                    u.Password == password);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}