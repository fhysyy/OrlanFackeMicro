using Npgsql;using System;using System.Security.Cryptography;using System.Text;

class Program
{
    static void Main()
    {
        // 从appsettings.json获取连接字符串
        string connectionString = "Host=localhost;Port=5432;Database=fakemicro;Username=postgres;Password=123456;SearchPath=public";

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            Console.WriteLine("Connected to PostgreSQL database!");

            // 查询admin用户信息
            string query = "SELECT id, username, email, password_hash, password_salt, is_active, role FROM users WHERE username = @username";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("username", "admin");
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Console.WriteLine($"User ID: {reader["id"]}");
                        Console.WriteLine($"Username: {reader["username"]}");
                        Console.WriteLine($"Email: {reader["email"]}");
                        Console.WriteLine($"Password Hash: {reader["password_hash"]}");
                        Console.WriteLine($"Password Salt: {reader["password_salt"]}");
                        Console.WriteLine($"Is Active: {reader["is_active"]}");
                        Console.WriteLine($"Role: {reader["role"]}");
                        Console.WriteLine();

                        // 测试密码验证
                        string password = "admin123";
                        string storedHash = reader["password_hash"].ToString();
                        string storedSalt = reader["password_salt"].ToString();

                        // 生成两种哈希值进行比较
                        var (hash1, salt1) = GeneratePasswordHash1(password); // DatabaseInitializerHostedService
                        var (hash2, salt2) = GeneratePasswordHash2(password); // AuthGrain

                        Console.WriteLine("DatabaseInitializerHostedService生成的哈希:");
                        Console.WriteLine($"Hash: {hash1}");
                        Console.WriteLine($"Salt: {salt1}");
                        Console.WriteLine();

                        Console.WriteLine("AuthGrain生成的哈希:");
                        Console.WriteLine($"Hash: {hash2}");
                        Console.WriteLine($"Salt: {salt2}");
                        Console.WriteLine();

                        Console.WriteLine($"两个哈希是否相同: {hash1 == hash2}");
                        Console.WriteLine($"两个盐值是否相同: {salt1 == salt2}");
                        Console.WriteLine();

                        // 使用两种方法验证密码
                        bool verify1 = VerifyPasswordHash1(password, storedHash, storedSalt);
                        bool verify2 = VerifyPasswordHash2(password, storedHash, storedSalt);

                        Console.WriteLine($"使用DatabaseInitializerHostedService验证密码结果: {verify1}");
                        Console.WriteLine($"使用AuthGrain验证密码结果: {verify2}");
                        Console.WriteLine();

                        // 测试密码
                        Console.WriteLine($"Password 'admin123' verification result with VerifyPasswordHash1: {verify1}");
                    }
                    else
                    {
                        Console.WriteLine("Admin user not found! Database initialization may have failed.");
                    }
                }
            }

            // 显示所有用户
            Console.WriteLine("All users in database:");
            string allUsersQuery = "SELECT id, username, email, is_active, role FROM users";
            using (var command = new NpgsqlCommand(allUsersQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"ID: {reader["id"]}, Username: {reader["username"]}, Email: {reader["email"]}, Active: {reader["is_active"]}, Role: {reader["role"]}");
                    }
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    // Copy of DatabaseInitializerHostedService.GeneratePasswordHash
    private static (string passwordHash, string passwordSalt) GeneratePasswordHash1(string password)
    {
        using var hmac = new HMACSHA512();
        var passwordSalt = Convert.ToBase64String(hmac.Key);
        var passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return (passwordHash, passwordSalt);
    }

    // Copy of AuthGrain.GeneratePasswordHash
    private static (string passwordHash, string passwordSalt) GeneratePasswordHash2(string password)
    {
        using var hmac = new HMACSHA512();
        var passwordSalt = Convert.ToBase64String(hmac.Key);
        var passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return (passwordHash, passwordSalt);
    }

    // Copy of DatabaseInitializerHostedService.VerifyPasswordHash
    private static bool VerifyPasswordHash1(string password, string storedHash, string storedSalt)
    {
        using var hmac = new HMACSHA512(Convert.FromBase64String(storedSalt));
        var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return computedHash == storedHash;
    }

    // Copy of AuthGrain.VerifyPasswordHash
    private static bool VerifyPasswordHash2(string password, string storedHash, string storedSalt)
    {
        using var hmac = new HMACSHA512(Convert.FromBase64String(storedSalt));
        var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return computedHash == storedHash;
    }
}