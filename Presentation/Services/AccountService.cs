using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

    public override async Task<GetAccountsReply> GetAccounts(GetAccountsRequest request, ServerCallContext context)
    {
        var users = await _userManager.Users.ToListAsync();

        var reply = new GetAccountsReply { Succeeded = true, Message = users.Count > 0 ? "Account retrieved successfully." : "No accounts found." };

        foreach (var user in users)
        {
            reply.Accounts.Add(new Account
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? "",
            });
        }

        return reply;
    }

    public override async Task<GetAccountReply> GetAccount(GetAccountRequest request, ServerCallContext context)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new GetAccountReply { Succeeded = false, Message = "No account found." };
        }

        var account = new Account
        {
            UserId = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? "",
        };

        return new GetAccountReply { Succeeded = true, Account = account, Message = "Account was found." };
    }

}
