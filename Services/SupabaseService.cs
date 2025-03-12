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
                diagnosticResults.AppendLine("Test 3: Querying 'students' table via Supabase client");
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

                    // Check if students table exists
                    diagnosticResults.AppendLine("Checking if 'students' table exists:");
                    var tableCheckSql = @"
                        SELECT EXISTS (
                            SELECT FROM information_schema.tables 
                            WHERE table_schema = 'public'
                            AND table_name = 'students'
                        );";

                    using var tableCommand = new NpgsqlCommand(tableCheckSql, connection);
                    var tableExists = (bool)await tableCommand.ExecuteScalarAsync();
                    diagnosticResults.AppendLine($"Students table exists: {tableExists}");

                    if (tableExists)
                    {
                        // Count students
                        var countSql = "SELECT COUNT(*) FROM students;";
                        using var countCommand = new NpgsqlCommand(countSql, connection);
                        var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                        diagnosticResults.AppendLine($"Student count: {count}");

                        // Get some students
                        if (count > 0)
                        {
                            var selectSql = "SELECT id, first_name, last_name, email FROM students LIMIT 3;";
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
                        diagnosticResults.AppendLine("Attempting to create students table...");
                        var createTableSql = @"
                            CREATE TABLE IF NOT EXISTS students (
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
                    await _supabaseClient.From<Student>()
                        .Filter("id", Postgrest.Constants.Operator.Equals, insertedStudent.Id)
                        .Delete();
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
            var students = new List<Student>();

            // Approach 1: Use Supabase Client
            try
            {
                _logger.LogInformation("Attempt 1: Fetching students using Supabase client");
                var response = await _supabaseClient.From<Student>().Get();
                _logger.LogInformation("Supabase client successful, found {Count} students", response.Models.Count);
                if (response.Models.Count > 0)
                {
                    return response.Models;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students using Supabase client");
            }

            // Approach 2: Use raw SQL through Supabase
            try
            {
                _logger.LogInformation("Attempt 2: Fetching students using Supabase raw query");
                var query = "SELECT * FROM students";
                var response = await _supabaseClient.Rpc("pgfunction_get_students", new Dictionary<string, object>());

                if (response != null && !string.IsNullOrEmpty(response.Content))
                {
                    _logger.LogInformation("Response content: {Content}", response.Content);
                    try
                    {
                        var results = JsonSerializer.Deserialize<List<Student>>(response.Content);
                        if (results != null && results.Count > 0)
                        {
                            _logger.LogInformation("SQL query successful, found {Count} students", results.Count);
                            return results;
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error deserializing JSON response");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students using Supabase SQL query");
            }

            // Approach 3: Direct database connection
            try
            {
                if (!string.IsNullOrEmpty(_connectionString))
                {
                    _logger.LogInformation("Attempt 3: Fetching students using direct database connection");
                    using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var query = "SELECT id, first_name, last_name, email, enrollment_date FROM students";
                    using var command = new NpgsqlCommand(query, connection);
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        students.Add(new Student
                        {
                            Id = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            Email = reader.GetString(3),
                            EnrollmentDate = reader.GetDateTime(4)
                        });
                    }

                    _logger.LogInformation("Direct connection successful, found {Count} students", students.Count);
                    if (students.Count > 0)
                    {
                        return students;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students using direct database connection");
            }

            // Approach 4: Try to create a sample student to verify table exists
            try
            {
                _logger.LogInformation("Attempt 4: Creating a sample student to verify connectivity");

                // First, check if the table exists
                try
                {
                    using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync();

                    // Try to create the table if it doesn't exist
                    var createTableSql = @"
                        CREATE TABLE IF NOT EXISTS students (
                            id SERIAL PRIMARY KEY,
                            first_name TEXT NOT NULL,
                            last_name TEXT NOT NULL,
                            email TEXT NOT NULL,
                            enrollment_date DATE NOT NULL,
                            created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                            updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
                        );";

                    using var createCmd = new NpgsqlCommand(createTableSql, connection);
                    await createCmd.ExecuteNonQueryAsync();

                    // Insert a sample student
                    var insertSql = @"
                        INSERT INTO students (first_name, last_name, email, enrollment_date)
                        VALUES ('Test', 'Student', 'test@example.com', CURRENT_DATE)
                        RETURNING id, first_name, last_name, email, enrollment_date;";

                    using var insertCmd = new NpgsqlCommand(insertSql, connection);
                    using var reader = await insertCmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        var sampleStudent = new Student
                        {
                            Id = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            Email = reader.GetString(3),
                            EnrollmentDate = reader.GetDateTime(4)
                        };

                        students.Add(sampleStudent);
                        _logger.LogInformation("Created sample student with ID: {Id}", sampleStudent.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create and retrieve sample student");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sample student creation");
            }

            // Return what we have, even if it's an empty list
            _logger.LogWarning("All approaches to fetch students failed! Returning {Count} students", students.Count);
            return students;
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            try
            {
                var response = await _supabaseClient.From<Student>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, id)
                    .Get();
                return response.Models.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student with ID: {Id}", id);
                return null;
            }
        }

        public async Task<Student?> CreateStudentAsync(Student student)
        {
            _logger.LogInformation("Attempting to create student: {FirstName} {LastName}", student.FirstName, student.LastName);

            // Approach 1: Use Supabase Client
            try
            {
                student.Id = 0; // Ensure ID is 0 to let the database generate it
                var response = await _supabaseClient.From<Student>().Insert(student);
                var createdStudent = response.Models.FirstOrDefault();

                if (createdStudent != null)
                {
                    _logger.LogInformation("Successfully created student with ID: {Id} using Supabase client", createdStudent.Id);
                    return createdStudent;
                }
                else
                {
                    _logger.LogWarning("Supabase client returned null after student creation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student using Supabase client");
            }

            // Approach 2: Use direct database connection
            try
            {
                if (!string.IsNullOrEmpty(_connectionString))
                {
                    using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        INSERT INTO students (first_name, last_name, email, enrollment_date)
                        VALUES (@firstName, @lastName, @email, @enrollmentDate)
                        RETURNING id, first_name, last_name, email, enrollment_date;";

                    using var command = new NpgsqlCommand(sql, connection);
                    command.Parameters.AddWithValue("firstName", student.FirstName);
                    command.Parameters.AddWithValue("lastName", student.LastName);
                    command.Parameters.AddWithValue("email", student.Email);
                    command.Parameters.AddWithValue("enrollmentDate", student.EnrollmentDate);

                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        var createdStudent = new Student
                        {
                            Id = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            Email = reader.GetString(3),
                            EnrollmentDate = reader.GetDateTime(4)
                        };

                        _logger.LogInformation("Successfully created student with ID: {Id} using direct SQL", createdStudent.Id);
                        return createdStudent;
                    }
                    else
                    {
                        _logger.LogWarning("Direct SQL insertion did not return a student record");
                    }
                }
                else
                {
                    _logger.LogWarning("Cannot use direct SQL - connection string is empty");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student using direct SQL");
            }

            return null;
        }

        public async Task<Student?> UpdateStudentAsync(Student student)
        {
            _logger.LogInformation("Attempting to update student with ID: {Id}", student.Id);

            // Approach 1: Use Supabase Client
            try
            {
                var response = await _supabaseClient.From<Student>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, student.Id)
                    .Update(student);

                var updatedStudent = response.Models.FirstOrDefault();

                if (updatedStudent != null)
                {
                    _logger.LogInformation("Successfully updated student with ID: {Id} using Supabase client", updatedStudent.Id);
                    return updatedStudent;
                }
                else
                {
                    _logger.LogWarning("Supabase client returned null after student update");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student with ID: {Id} using Supabase client", student.Id);
            }

            // Approach 2: Use direct database connection
            try
            {
                if (!string.IsNullOrEmpty(_connectionString))
                {
                    using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        UPDATE students 
                        SET first_name = @firstName, last_name = @lastName, email = @email, enrollment_date = @enrollmentDate
                        WHERE id = @id
                        RETURNING id, first_name, last_name, email, enrollment_date;";

                    using var command = new NpgsqlCommand(sql, connection);
                    command.Parameters.AddWithValue("id", student.Id);
                    command.Parameters.AddWithValue("firstName", student.FirstName);
                    command.Parameters.AddWithValue("lastName", student.LastName);
                    command.Parameters.AddWithValue("email", student.Email);
                    command.Parameters.AddWithValue("enrollmentDate", student.EnrollmentDate);

                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        var updatedStudent = new Student
                        {
                            Id = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            Email = reader.GetString(3),
                            EnrollmentDate = reader.GetDateTime(4)
                        };

                        _logger.LogInformation("Successfully updated student with ID: {Id} using direct SQL", updatedStudent.Id);
                        return updatedStudent;
                    }
                    else
                    {
                        _logger.LogWarning("Direct SQL update did not return a student record");
                    }
                }
                else
                {
                    _logger.LogWarning("Cannot use direct SQL - connection string is empty");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student with ID: {Id} using direct SQL", student.Id);
            }

            return null;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            _logger.LogInformation("Attempting to delete student with ID: {Id}", id);

            // Approach 1: Use Supabase Client
            try
            {
                await _supabaseClient.From<Student>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, id)
                    .Delete();

                _logger.LogInformation("Successfully deleted student with ID: {Id} using Supabase client", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student with ID: {Id} using Supabase client", id);
            }

            // Approach 2: Use direct database connection
            try
            {
                if (!string.IsNullOrEmpty(_connectionString))
                {
                    using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "DELETE FROM students WHERE id = @id";

                    using var command = new NpgsqlCommand(sql, connection);
                    command.Parameters.AddWithValue("id", id);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation("Successfully deleted student with ID: {Id} using direct SQL", id);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Student with ID: {Id} not found for deletion using direct SQL", id);
                    }
                }
                else
                {
                    _logger.LogWarning("Cannot use direct SQL - connection string is empty");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student with ID: {Id} using direct SQL", id);
            }

            return false;
        }
    }
}
