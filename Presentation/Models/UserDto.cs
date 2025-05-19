using Microsoft.AspNetCore.Identity;

namespace Presentation.Models;

public class UserDto : IdentityUser
{
    public string UserId { get; set; } = null!;
    public string Role {  get; set; } = null!;
}
