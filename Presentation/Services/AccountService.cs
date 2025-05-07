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

    public override async Task<ValidateCredentialsReply> ValidateCredentials(ValidateCredentialsRequest request, ServerCallContext context)
    {

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new ValidateCredentialsReply { Succeeded = false, Message = "Email and password must be provided." };
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new ValidateCredentialsReply { Succeeded = false, Message = "Invalid credentials." };
        }

        var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValid)
        {
            return new ValidateCredentialsReply { Succeeded = false, Message = "Invalid credentials" };
        }

        return new ValidateCredentialsReply { Succeeded = true, Message = "Login successful", UserId = user.Id };
    }

    public override async Task<UpdatePhoneNumberReply> UpdatePhoneNumber(UpdatePhoneNumberRequest request, ServerCallContext context)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new UpdatePhoneNumberReply { Succeeded = false, Message = "No account found." };
        }

        if (!string.Equals(user.PhoneNumber, request.PhoneNumber, StringComparison.Ordinal))
        {
            user.PhoneNumber = request.PhoneNumber;
        }

        var result = await _userManager.UpdateAsync(user);

        return new UpdatePhoneNumberReply
        {
            Succeeded = result.Succeeded,
            Message = result.Succeeded
                ? "Account was updated successfully."
                : string.Join(", ", result.Errors.Select(e => e.Description))
        };
    }

    public override async Task<DeleteAccountByIdReply> DeleteAccountById(DeleteAccountByIdRequest request, ServerCallContext context)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new DeleteAccountByIdReply { Succeeded = false, Message = "No account found." };
        }

        var result = await _userManager.DeleteAsync(user);

        return new DeleteAccountByIdReply
        {
            Succeeded = result.Succeeded,
            Message = result.Succeeded
                ? "Account was deleted successfully."
                : string.Join(", ", result.Errors.Select(e => e.Description))
        };
    }

    public override async Task<ConfirmAccountReply> ConfirmAccount(ConfirmAccountRequest request, ServerCallContext context)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new ConfirmAccountReply { Succeeded = false, Message = "No account found." };
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            return new ConfirmAccountReply { Succeeded = true, Message = "Account is already confirmed." };
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);

        return new ConfirmAccountReply
        {
            Succeeded = result.Succeeded,
            Message = result.Succeeded
                ? "Email confirmed successfully."
                : string.Join(", ", result.Errors.Select(e => e.Description))
        };
    }
}
