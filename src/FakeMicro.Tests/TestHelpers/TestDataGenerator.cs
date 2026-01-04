using FakeMicro.Entities;
using FakeMicro.Interfaces.Models;
using System;
using System.Collections.Generic;

namespace FakeMicro.Tests.TestHelpers;

public static class TestDataGenerator
{
    private static readonly Random _random = new();

    public static User CreateTestUser(long id = 1, string username = "testuser")
    {
        return new User
        {
            id = id,
            username = username,
            email = $"{username}@test.com",
            password_hash = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Message CreateTestMessage(long id = 1, long senderId = 1, long receiverId = 2)
    {
        return new Message
        {
            id = id,
            sender_id = senderId,
            receiver_id = receiverId,
            title = $"Test message {id}",
            content = $"Test content for message {id}",
            message_type = "Notification",
            message_channel = "InApp",
            status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Note CreateTestNote(long id = 1, long userId = 1)
    {
        return new Note
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            NotebookId = Guid.NewGuid(),
            Title = $"Test Note {id}",
            ContentHash = "testhash",
            ContentCrdt = Array.Empty<byte>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Notebook CreateTestNotebook(long id = 1, long userId = 1)
    {
        return new Notebook
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ParentId = null,
            Title = $"Test Notebook {id}",
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static FakeStudent CreateTestFakeStudent(long id = 1)
    {
        return new FakeStudent
        {
            Id = id,
            ClassName = $"Class {id}",
            ClassCode = $"C{id:D3}",
            Grade = _random.Next(1, 6),
            Major = "Computer Science",
            TeacherId = 1,
            MaxStudents = 50,
            CurrentStudents = _random.Next(1, 50),
            Status = 1,
            Description = $"Test class {id}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static RegisterRequest CreateCreateUserRequest(string username = "testuser")
    {
        return new RegisterRequest
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "TestPassword123!"
        };
    }

    public static UpdateUserRequest CreateUpdateUserRequest()
    {
        return new UpdateUserRequest
        {
            Username = "updateduser",
            Email = "updated@test.com",
            Phone = "1234567890"
        };
    }

    public static PagedResult<T> CreatePagedResult<T>(List<T> items, int totalCount = 10, int page = 1, int pageSize = 10)
    {
        return new PagedResult<T>
        {
            Data = items,
            TotalCount = totalCount,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public static List<User> CreateTestUsers(int count)
    {
        var users = new List<User>();
        for (int i = 1; i <= count; i++)
        {
            users.Add(CreateTestUser(i, $"user{i}"));
        }
        return users;
    }

    public static List<Message> CreateTestMessages(int count, long senderId = 1, long receiverId = 2)
    {
        var messages = new List<Message>();
        for (int i = 1; i <= count; i++)
        {
            messages.Add(CreateTestMessage(i, senderId, receiverId));
        }
        return messages;
    }

    public static string GenerateRandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public static int GenerateRandomInt(int min = 1, int max = 100)
    {
        return _random.Next(min, max + 1);
    }

    public static DateTime GenerateRandomDate(DateTime? start = null, DateTime? end = null)
    {
        var startDate = start ?? DateTime.UtcNow.AddYears(-1);
        var endDate = end ?? DateTime.UtcNow;
        var range = (endDate - startDate).Days;
        return startDate.AddDays(_random.Next(range));
    }
}
