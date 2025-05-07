using Microsoft.AspNetCore.Identity;

namespace Presentation.Services;

public class AccountService(UserManager<IdentityUser> userManager)
{
    private readonly UserManager<IdentityUser> _userManager = userManager;

}
