using StudentInformationSystem.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;
using System.Text;
using System.Linq;

namespace StudentInformationSystem.Services
{
    public class SupabaseService
    {
        private readonly Client _supabaseClient;
        private readonly ILogger<SupabaseService> _logger;
        private readonly string _connectionString;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;

        public SupabaseService(IConfiguration configuration, ILogger<SupabaseService> logger)
        {
            _logger = logger;
            _supabaseUrl = configuration["Supabase:Url"] ?? "";
            _supabaseKey = configuration["Supabase:Key"] ?? "";
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

            _logger.LogInformation("Initializing Supabase with URL: {Url}", _supabaseUrl);
            _logger.LogInformation("Connection string is set: {IsSet}", !string.IsNullOrEmpty(_connectionString));

            if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_supabaseKey))
            {
                _logger.LogError("Supabase URL or Key is missing in configuration");
                throw new ArgumentException("Supabase URL and Key must be configured in appsettings.json");
            }

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true
            };

            _supabaseClient = new Client(_supabaseUrl, _supabaseKey, options);
            _logger.LogInformation("Supabase client initialized successfully");
        }

        public async Task<string> TestConnectionAsync()
        {
            var diagnosticResults = new StringBuilder();
            diagnosticResults.AppendLine("--- Supabase Connection Diagnostic Report ---");
            diagnosticResults.AppendLine($"Timestamp: {DateTime.Now}");
            diagnosticResults.AppendLine($"Supabase URL: {_supabaseUrl}");
            diagnosticResults.AppendLine($"Supabase Key: {_supabaseKey.Substring(0, 10)}... (truncated for security)");
            diagnosticResults.AppendLine($"Connection String: {(_connectionString.Length > 0 ? "Provided" : "Not provided")}");
            diagnosticResults.AppendLine();

            // Test 1: Check if we can connect to Supabase at all
            try
            {
                diagnosticResults.AppendLine("Test 1: Testing base Supabase connection");
                var healthCheck = await _supabaseClient.Rpc("ping", new Dictionary<string, object>());
                diagnosticResults.AppendLine($"Connection successful: {healthCheck != null}");
                if (healthCheck != null)
                {
                    diagnosticResults.AppendLine($"Response: {healthCheck.Content}");
                }
            }
            catch (Exception ex)
            {
                diagnosticResults.AppendLine($"Connection failed: {ex.Message}");
                diagnosticResults.AppendLine($"Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    diagnosticResults.AppendLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            diagnosticResults.AppendLine();

            // Test 2: Try to get the list of tables
            try
            {
                diagnosticResults.AppendLine("Test 2: Checking database tables");
                var tablesResponse = await _supabaseClient.Rpc("get_tables_info", new Dictionary<string, object>());
                diagnosticResults.AppendLine($"Response received: {tablesResponse != null}");
                if (tablesResponse != null)
                {
                    diagnosticResults.AppendLine($"Response content: {tablesResponse.Content}");
                }
            }
            catch (Exception ex)
            {
                diagnosticResults.AppendLine($"Failed to get tables: {ex.Message}");
            }
            diagnosticResults.AppendLine();

            // Test 3: Try to fetch students from Supabase
            try
            {
                diagnosticResults.AppendLine("Test 3: Querying 'Students' table via Supabase client");
                var response = await _supabaseClient.From<Student>().Get();
                diagnosticResults.AppendLine($"Query successful, found {response.Models.Count} students");
                if (response.Models.Count > 0)
                {
                    diagnosticResults.AppendLine("First few students:");
                    foreach (var student in response.Models.Take(3))
                    {
                        diagnosticResults.AppendLine($"ID: {student.Id}, Name: {student.FirstName} {student.LastName}, Email: {student.Email}");
                    }
                }
                else
                {
                    diagnosticResults.AppendLine("No students found in the Supabase table.");
                }
            }
            catch (Exception ex)
            {
                diagnosticResults.AppendLine($"Query failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    diagnosticResults.AppendLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            diagnosticResults.AppendLine();

            // Test 4: Try direct SQL connection
            if (!string.IsNullOrEmpty(_connectionString))
            {
                try
                {
                    diagnosticResults.AppendLine("Test 4: Testing direct SQL connection");
                    using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync();
                    diagnosticResults.AppendLine("Connection opened successfully");

                    // Check if Students table exists
                    diagnosticResults.AppendLine("Checking if 'Students' table exists:");
                    var tableCheckSql = @"
                        SELECT EXISTS (
                            SELECT FROM information_schema.tables 
                            WHERE table_schema = 'public'
                            AND table_name = 'Students'
                        );";

                    using var tableCommand = new NpgsqlCommand(tableCheckSql, connection);
                    var tableExists = (bool)await tableCommand.ExecuteScalarAsync();
                    diagnosticResults.AppendLine($"Students table exists: {tableExists}");

                    if (tableExists)
                    {
                        // Count students
                        var countSql = "SELECT COUNT(*) FROM Students;";
                        using var countCommand = new NpgsqlCommand(countSql, connection);
                        var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                        diagnosticResults.AppendLine($"Student count: {count}");

                        // Get some students
                        if (count > 0)
                        {
                            var selectSql = "SELECT id, first_name, last_name, email FROM Students LIMIT 3;";
                            using var selectCommand = new NpgsqlCommand(selectSql, connection);
                            using var reader = await selectCommand.ExecuteReaderAsync();

                            diagnosticResults.AppendLine("First few students via direct SQL:");
                            int studentNum = 0;
                            while (await reader.ReadAsync())
                            {
                                studentNum++;
                                var id = reader.GetInt32(0);
                                var firstName = reader.GetString(1);
                                var lastName = reader.GetString(2);
                                var email = reader.GetString(3);
                                diagnosticResults.AppendLine($"ID: {id}, Name: {firstName} {lastName}, Email: {email}");
                            }

                            if (studentNum == 0)
                            {
                                diagnosticResults.AppendLine("No students returned from direct SQL query even though count was > 0.");
                            }
                        }
                    }
                    else
                    {
                        // If the table doesn't exist, try to create it
                        diagnosticResults.AppendLine("Attempting to create Students table...");
                        var createTableSql = @"
                            CREATE TABLE IF NOT EXISTS Students (
                                id SERIAL PRIMARY KEY,
                                first_name TEXT NOT NULL,
                                last_name TEXT NOT NULL,
                                email TEXT NOT NULL,
                                enrollment_date DATE NOT NULL,
                                created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                                updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
                            );";

                        using var createCommand = new NpgsqlCommand(createTableSql, connection);
                        await createCommand.ExecuteNonQueryAsync();
                        diagnosticResults.AppendLine("Table creation command executed. Checking if table exists now...");

                        using var recheckCommand = new NpgsqlCommand(tableCheckSql, connection);
                        tableExists = (bool)await recheckCommand.ExecuteScalarAsync();
                        diagnosticResults.AppendLine($"Students table exists after creation attempt: {tableExists}");
                    }
                }
                catch (Exception ex)
                {
                    diagnosticResults.AppendLine($"Direct SQL connection failed: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        diagnosticResults.AppendLine($"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }
            else
            {
                diagnosticResults.AppendLine("Test 4: Skipped - No connection string provided");
            }
            diagnosticResults.AppendLine();

            // Test 5: Try to insert a test record to verify write access
            try
            {
                diagnosticResults.AppendLine("Test 5: Testing write access by inserting a test student");

                var testStudent = new Student
                {
                    FirstName = "Test",
                    LastName = "User_" + DateTime.Now.ToString("yyyyMMddHHmmss"), // Add timestamp to avoid duplicates
                    Email = $"test_{DateTime.Now.Ticks}@example.com",
                    EnrollmentDate = DateTime.Now
                };

                var response = await _supabaseClient.From<Student>().Insert(testStudent);
                bool success = response != null && response.Models.Count > 0;

                diagnosticResults.AppendLine($"Insert test successful: {success}");
                if (success)
                {
                    var insertedStudent = response.Models[0];
                    diagnosticResults.AppendLine($"Inserted student ID: {insertedStudent.Id}, Name: {insertedStudent.FirstName} {insertedStudent.LastName}");

                    // Try to delete the test record
                    diagnosticResults.AppendLine("Attempting to delete the test student...");
                    await _supabaseClient.From<Student>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, insertedStudent.Id).Delete();
                    diagnosticResults.AppendLine("Delete command sent. If there are no errors, the deletion was successful.");
                }
            }
            catch (Exception ex)
            {
                diagnosticResults.AppendLine($"Write test failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    diagnosticResults.AppendLine($"Inner exception: {ex.InnerException.Message}");
                }
            }

            return diagnosticResults.ToString();
        }

        public async Task<List<Student>> GetStudentsAsync()
        {
            try
            {
                var response = await _supabaseClient.From<Student>().Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students from Supabase");
                return new List<Student>();
            }
        }

        public async Task<List<Course>> GetCoursesAsync()
        {
            try
            {
                var response = await _supabaseClient.From<Course>().Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching courses from Supabase");
                return new List<Course>();
            }
        }

        public async Task<List<Enrollment>> GetEnrollmentsAsync()
        {
            try
            {
                var response = await _supabaseClient.From<Enrollment>().Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrollments from Supabase");
                return new List<Enrollment>();
            }
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            try
            {
                var response = await _supabaseClient.From<Student>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                    .Get();

                if (response?.Models != null && response.Models.Any())
                {
                    _logger.LogInformation("Successfully retrieved student with ID {Id}", id);
                    return response.Models.First();
                }
                _logger.LogWarning("No student found with ID {Id}", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student with ID {Id} from Supabase", id);
                throw;
            }
        }

        public async Task<Student?> CreateStudentAsync(Student student)
        {
            try
            {
                if (student == null)
                {
                    _logger.LogError("Attempted to create a null student");
                    return null;
                }

                var response = await _supabaseClient.From<Student>().Insert(student);
                if (response?.Models != null && response.Models.Any())
                {
                    var createdStudent = response.Models.First();
                    _logger.LogInformation("Successfully created student with ID {Id}", createdStudent.Id);
                    return createdStudent;
                }
                _logger.LogError("Failed to create student - no response received");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student in Supabase");
                throw;
            }
        }

        public async Task<Student?> UpdateStudentAsync(Student student)
        {
            try
            {
                if (student == null || student.Id <= 0)
                {
                    _logger.LogError("Attempted to update an invalid student");
                    return null;
                }

                var response = await _supabaseClient.From<Student>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, student.Id)
                    .Update(student);

                if (response?.Models != null && response.Models.Any())
                {
                    var updatedStudent = response.Models.First();
                    _logger.LogInformation("Successfully updated student with ID {Id}", updatedStudent.Id);
                    return updatedStudent;
                }
                _logger.LogError("Failed to update student - no response received");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student in Supabase");
                throw;
            }
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogError("Attempted to delete student with invalid ID {Id}", id);
                    return false;
                }

                await _supabaseClient.From<Student>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                    .Delete();

                _logger.LogInformation("Successfully deleted student with ID {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student from Supabase");
                throw;
            }
        }
    }
}
