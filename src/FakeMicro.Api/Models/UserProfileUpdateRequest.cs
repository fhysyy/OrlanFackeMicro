using System.Collections.Generic;

namespace FakeMicro.Api.Models;

public class UserProfileUpdateRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
}