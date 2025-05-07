using Grpc.Core;
using Microsoft.AspNetCore.Identity;

namespace Presentation.Services;

public class AccountService(UserManager<IdentityUser> userManager) : AccountGrpcService.AccountGrpcServiceBase
{
    private readonly UserManager<IdentityUser> _userManager = userManager;

    public override async Task<CreateAccountReply> CreateAccount(CreateAccountRequest request, ServerCallContext context)
    {
        var user = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        var reply = new CreateAccountReply
        {
            Succeeded = result.Succeeded,
            Message = result.Succeeded ? "Account was created successfully." : string.Join(", ", result.Errors.Select(e => e.Description))
        };

        if (result.Succeeded)
        {
            reply.UserId = user.Id;
        }

        return reply;
    }

}
