using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.AspNetCore.Identity;
using Moq;
using Presentation;
using Presentation.Services;
using Presentation_Tests.Helpers;

namespace Presentation_Tests.Services;

public class AccountService_Tests
{
    private static Mock<UserManager<IdentityUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null, null, null, null, null, null, null, null);
    }

    private static Mock<RoleManager<IdentityRole>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object, null, null, null, null);
    }

    private static ServerCallContext CreateTestServerCallContext()
    {
        return TestServerCallContext.Create(
            method: "TestMethod",
            host: null,
            deadline: DateTime.UtcNow.AddMinutes(1),
            requestHeaders: new Metadata(),
            cancellationToken: CancellationToken.None,
            peer: null,
            authContext: null,
            contextPropagationToken: null,
            writeHeadersFunc: _ => Task.CompletedTask,
            writeOptionsGetter: () => null,
            writeOptionsSetter: _ => { }
        );
    }

    // Create account ---------------------------------------------------------

    [Fact]
    public async Task CreateAccount_ShouldSucceed_WhenUserIsCreatedSuccessfully()
    {
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), "BytMig123!"))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        roleManagerMock.Setup(x => x.RoleExistsAsync("User")).ReturnsAsync(true);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);

        var request = new CreateAccountRequest
        {
            Email = "test@domain.com",
            Password = "BytMig123!"
        };

        var context = CreateTestServerCallContext();

        var result = await service.CreateAccount(request, context);

        Assert.True(result.Succeeded);
        Assert.Equal("Account was created successfully.", result.Message);
    }

    [Fact]
    public async Task CreateAccount_ShouldFail_WhenUserCreationFails()
    {
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        var identityErrors = new List<IdentityError>
        {
            new IdentityError { Description = "Account creation failed. Password is too weak." }
        };

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

        roleManagerMock.Setup(x => x.RoleExistsAsync("User")).ReturnsAsync(true);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);

        var request = new CreateAccountRequest
        {
            Email = "test@domain.com",
            Password = "weak"
        };

        var context = CreateTestServerCallContext();

        var result = await service.CreateAccount(request, context);

        Assert.False(result.Succeeded);
        Assert.Equal("Account creation failed. Password is too weak.", result.Message);
    }

    // Get accounts ---------------------------------------------------------

    [Fact]
    public async Task GetAccounts_ShouldReturnAccounts_WhenUsersExist()
    {
        // Arrange
        var user1 = new IdentityUser { Id = "1", UserName = "Björn", Email = "bjorn@domain.com", PhoneNumber = "0736123456" };
        var user2 = new IdentityUser { Id = "2", UserName = "Desirée", Email = "dessi@domain.com" };

        var users = new List<IdentityUser> { user1, user2 };

        var mockUserManager = MockUserManager();

        mockUserManager.Setup(u => u.Users).Returns(new TestAsyncEnumerable<IdentityUser>(users));

        mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user1);
        mockUserManager.Setup(u => u.FindByIdAsync("2")).ReturnsAsync(user2);

        mockUserManager.Setup(u => u.GetRolesAsync(user1))
            .ReturnsAsync(new List<string> { "Admin" });

        mockUserManager.Setup(u => u.GetRolesAsync(user2))
            .ReturnsAsync(new List<string> { "User" });

        var service = new AccountService(mockUserManager.Object, MockRoleManager().Object);
        var context = CreateTestServerCallContext();

        // Act
        var result = await service.GetAccounts(new GetAccountsRequest(), context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Accounts.Count);
        Assert.Equal("Björn", result.Accounts[0].UserName);
        Assert.Equal("Admin", result.Accounts[0].RoleName);
        Assert.Equal("Desirée", result.Accounts[1].UserName);
        Assert.Equal("User", result.Accounts[1].RoleName);
    }


    [Fact]
    public async Task GetAccounts_ShouldReturnFailure_WhenExceptionIsThrown()
    {
        var mockUserManager = MockUserManager();
        mockUserManager.Setup(u => u.Users).Throws(new Exception("Database connection failed."));

        var service = new AccountService(mockUserManager.Object, MockRoleManager().Object);
        var context = CreateTestServerCallContext();

        var result = await service.GetAccounts(new GetAccountsRequest(), context);

        Assert.False(result.Succeeded);
        Assert.Equal("Database connection failed.", result.Message);
        Assert.Empty(result.Accounts);
    }

    // Get account -----------------------------------------------------------
    [Fact]
    public async Task GetAccount_ShouldReturnSuccess_WhenUserExists()
    {
        // Arrange
        var user = new IdentityUser
        {
            Id = "123",
            UserName = "Björn",
            Email = "bjorn@example.com",
            PhoneNumber = "0701234567"
        };

        var mockUserManager = MockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync("123")).ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var service = new AccountService(mockUserManager.Object, MockRoleManager().Object);
        var context = CreateTestServerCallContext();

        var request = new GetAccountRequest { UserId = "123" };

        // Act
        var result = await service.GetAccount(request, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Account was found.", result.Message);
        Assert.NotNull(result.Account);
        Assert.Equal("123", result.Account.UserId);
        Assert.Equal("Björn", result.Account.UserName);
        Assert.Equal("bjorn@example.com", result.Account.Email);
        Assert.Equal("0701234567", result.Account.PhoneNumber);
        Assert.Equal("Admin", result.Account.RoleName);
    }

    [Fact]
    public async Task GetAccount_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        var mockUserManager = MockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityUser?)null);

        var service = new AccountService(mockUserManager.Object, MockRoleManager().Object);
        var context = CreateTestServerCallContext();

        var request = new GetAccountRequest { UserId = "nonexistent" };

        // Act
        var result = await service.GetAccount(request, context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("No account found.", result.Message);
        Assert.Null(result.Account);
    }

    // Validate credentials ---------------------------------------------------------
    [Fact]
    public async Task ValidateCredentials_ShouldFail_WhenEmailOrPasswordIsEmpty()
    {
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();
        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);

        var context = CreateTestServerCallContext();

        var request1 = new ValidateCredentialsRequest
        {
            Email = "",
            Password = "BytMig123!"
        };
        var result1 = await service.ValidateCredentials(request1, context);
        Assert.False(result1.Succeeded);
        Assert.Equal("Email and password must be provided.", result1.Message);

        var request2 = new ValidateCredentialsRequest
        {
            Email = "test@domain.com",
            Password = ""
        };
        var result2 = await service.ValidateCredentials(request2, context);
        Assert.False(result2.Succeeded);
        Assert.Equal("Email and password must be provided.", result2.Message);
    }

    [Fact]
    public async Task ValidateCredentials_ShouldSucceed_WhenCredentialsAreValid()
    {
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        var user = new IdentityUser { Id = "123", Email = "test@domain.com" };

        userManagerMock.Setup(x => x.FindByEmailAsync("test@domain.com"))
            .ReturnsAsync(user);

        userManagerMock.Setup(x => x.CheckPasswordAsync(user, "BytMig123!"))
            .ReturnsAsync(true);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new ValidateCredentialsRequest
        {
            Email = "test@domain.com",
            Password = "BytMig123!"
        };

        var result = await service.ValidateCredentials(request, context);

        Assert.True(result.Succeeded);
        Assert.Equal("Login successful", result.Message);
        Assert.Equal("123", result.UserId);
    }

    // Update phonenumber ---------------------------------------------
    [Fact]
    public async Task UpdatePhoneNumber_ShouldSucceed_WhenUserExistsAndUpdateSucceeds()
    {
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        var user = new IdentityUser
        {
            Id = "123",
            PhoneNumber = "0736123456"
        };

        userManagerMock.Setup(x => x.FindByIdAsync("123")).ReturnsAsync(user);

        userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<IdentityUser>()))
                       .ReturnsAsync(IdentityResult.Success);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new UpdatePhoneNumberRequest
        {
            UserId = "123",
            PhoneNumber = "0701234567"
        };

        var result = await service.UpdatePhoneNumber(request, context);

        Assert.True(result.Succeeded);
        Assert.Equal("Account was updated successfully.", result.Message);
        Assert.Equal("0701234567", user.PhoneNumber);
    }

    [Fact]
    public async Task UpdatePhoneNumber_ShouldFail_WhenUserNotFound()
    {
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        userManagerMock.Setup(x => x.FindByIdAsync("nonexistent"))
                       .ReturnsAsync((IdentityUser?)null);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new UpdatePhoneNumberRequest
        {
            UserId = "nonexistent",
            PhoneNumber = "0700000000"
        };

        var result = await service.UpdatePhoneNumber(request, context);

        Assert.False(result.Succeeded);
        Assert.Equal("No account found.", result.Message);
    }

    // Delete account ----------------------------------------------------
    [Fact]
    public async Task DeleteAccountById_ShouldSucceed_WhenUserExistsAndDeletionSucceeds()
    {
        // Arrange
        var userManagerMock = MockUserManager();

        var user = new IdentityUser { Id = "123" };

        userManagerMock.Setup(x => x.FindByIdAsync("123"))
            .ReturnsAsync(user);

        userManagerMock.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var service = new AccountService(userManagerMock.Object, MockRoleManager().Object);
        var context = CreateTestServerCallContext();

        var request = new DeleteAccountByIdRequest { UserId = "123" };

        // Act
        var result = await service.DeleteAccountById(request, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Account was deleted successfully.", result.Message);
    }

    [Fact]
    public async Task DeleteAccountById_ShouldFail_WhenUserDoesNotExist()
    {
        // Arrange
        var userManagerMock = MockUserManager();

        userManagerMock.Setup(x => x.FindByIdAsync("nonexistent"))
            .ReturnsAsync((IdentityUser?)null);

        var service = new AccountService(userManagerMock.Object, MockRoleManager().Object);
        var context = CreateTestServerCallContext();

        var request = new DeleteAccountByIdRequest { UserId = "nonexistent" };

        // Act
        var result = await service.DeleteAccountById(request, context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("No account found.", result.Message);
    }

    // Confirm account ---------------------------------------------------
    [Fact]
    public async Task ConfirmAccount_ShouldReturnSuccess_WhenEmailConfirmedSuccessfully()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        var user = new IdentityUser { Id = "123" };

        userManagerMock.Setup(x => x.FindByIdAsync("123")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(false);
        userManagerMock.Setup(x => x.ConfirmEmailAsync(user, "validToken"))
            .ReturnsAsync(IdentityResult.Success);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new ConfirmAccountRequest { UserId = "123", Token = "validToken" };

        // Act
        var result = await service.ConfirmAccount(request, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Email confirmed successfully.", result.Message);
    }

    [Fact]
    public async Task ConfirmAccount_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        userManagerMock.Setup(x => x.FindByIdAsync("nonexistent"))
            .ReturnsAsync((IdentityUser?)null);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new ConfirmAccountRequest { UserId = "nonexistent", Token = "dummyToken" };

        // Act
        var result = await service.ConfirmAccount(request, context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("No account found.", result.Message);
    }

    // Update email --------------------------------------------------
    [Fact]
    public async Task UpdateEmail_ShouldSucceed_WhenEmailIsUpdated()
    {
        // Arrange
        var userManagerMock = MockUserManager();

        var existingUser = new IdentityUser
        {
            Id = "123",
            Email = "bjorn@domain.com"
        };

        userManagerMock.Setup(x => x.FindByIdAsync("123"))
                       .ReturnsAsync(existingUser);

        userManagerMock.Setup(x => x.GenerateChangeEmailTokenAsync(existingUser, "bjorn.ahstrom@domain.com"))
                       .ReturnsAsync("mock-token");

        var service = new AccountService(userManagerMock.Object, MockRoleManager().Object);
        var context = CreateTestServerCallContext();

        var request = new UpdateEmailRequest
        {
            UserId = "123",
            NewEmail = "bjorn.ahstrom@domain.com"
        };

        // Act
        var result = await service.UpdateEmail(request, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Token generated from email change.", result.Message);
        Assert.Equal("mock-token", result.Token);
    }

    [Fact]
    public async Task UpdateEmail_ShouldReturnFailure_WhenUserNotFound()
    {
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        userManagerMock.Setup(x => x.FindByIdAsync("nonexistent"))
                       .ReturnsAsync((IdentityUser?)null);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new UpdateEmailRequest
        {
            UserId = "nonexistent",
            NewEmail = "bjorn.ahstrom@domain.com"
        };

        var result = await service.UpdateEmail(request, context);

        Assert.False(result.Succeeded);
        Assert.Equal("User not found.", result.Message);
    }

    // Confirm email change
    [Fact]
    public async Task ConfirmEmailChange_ShouldReturnSuccess_WhenEmailChangeIsConfirmed()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        var user = new IdentityUser { Id = "user123" };

        userManagerMock.Setup(x => x.FindByIdAsync("user123"))
                       .ReturnsAsync(user);

        userManagerMock.Setup(x => x.ChangeEmailAsync(user, "bjorn.ahstrom@domain.com", "validtoken"))
                       .ReturnsAsync(IdentityResult.Success);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new ConfirmEmailChangeRequest
        {
            UserId = "user123",
            NewEmail = "bjorn.ahstrom@domain.com",
            Token = "validtoken"
        };

        // Act
        var result = await service.ConfirmEmailChange(request, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Email confirmed successfully.", result.Message);
    }

    [Fact]
    public async Task ConfirmEmailChange_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        userManagerMock.Setup(x => x.FindByIdAsync("nonexistent"))
                       .ReturnsAsync((IdentityUser?)null);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new ConfirmEmailChangeRequest
        {
            UserId = "nonexistent",
            NewEmail = "bjorn.ahstrom@domain.com",
            Token = "validtoken"
        };

        // Act
        var result = await service.ConfirmEmailChange(request, context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User not found.", result.Message);
    }

    // Reset password
    [Fact]
    public async Task ResetPassword_ShouldReturnSuccess_WhenPasswordResetSucceeds()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        var user = new IdentityUser { Id = "user123" };

        userManagerMock.Setup(x => x.FindByIdAsync("user123")).ReturnsAsync(user);

        userManagerMock.Setup(x => x.ResetPasswordAsync(user, "validToken", "BytMig123!"))
                       .ReturnsAsync(IdentityResult.Success);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new ResetPasswordRequest
        {
            UserId = "user123",
            Token = "validToken",
            NewPassword = "BytMig123!"
        };

        // Act
        var result = await service.ResetPassword(request, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Password reset successfully.", result.Message);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        userManagerMock.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityUser?)null);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new ResetPasswordRequest
        {
            UserId = "nonexistent",
            Token = "anyToken",
            NewPassword = "NewPassword123"
        };

        // Act
        var result = await service.ResetPassword(request, context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User not found.", result.Message);
    }

    // Generate email confirmation token
    [Fact]
    public async Task GenerateEmailConfirmationToken_ShouldReturnSuccess_WhenUserExists()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        var user = new IdentityUser { Id = "user123" };
        var generatedToken = "token123";

        userManagerMock.Setup(x => x.FindByIdAsync("user123")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync(generatedToken);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new GenerateTokenRequest
        {
            UserId = "user123"
        };

        // Act
        var result = await service.GenerateEmailConfirmationToken(request, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(generatedToken, result.Token);
        Assert.Equal("Token generated successfully.", result.Message);
    }

    [Fact]
    public async Task GenerateEmailConfirmationToken_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        userManagerMock.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityUser?)null);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new GenerateTokenRequest
        {
            UserId = "nonexistent"
        };

        // Act
        var result = await service.GenerateEmailConfirmationToken(request, context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("No account found.", result.Message);
    }

    // Generate password reset token
    [Fact]
    public async Task GeneratePasswordResetToken_ShouldReturnSuccess_WhenUserExists()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        var user = new IdentityUser { Id = "user123" };
        var generatedToken = "reset-token-123";

        userManagerMock.Setup(x => x.FindByIdAsync("user123")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(generatedToken);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new GenerateTokenRequest
        {
            UserId = "user123"
        };

        // Act
        var result = await service.GeneratePasswordResetToken(request, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(generatedToken, result.Token);
        Assert.Equal("Password reset token generated.", result.Message);
    }

    [Fact]
    public async Task GeneratePasswordResetToken_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        userManagerMock.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityUser?)null);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new GenerateTokenRequest
        {
            UserId = "nonexistent"
        };

        // Act
        var result = await service.GeneratePasswordResetToken(request, context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User not found.", result.Message);
    }

    // Change user role
    [Fact]
    public async Task ChangeUserRole_ShouldReturnSuccess_WhenRoleChanged()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        var user = new IdentityUser { Id = "user123" };
        var currentRoles = new List<string> { "User" };

        userManagerMock.Setup(x => x.FindByIdAsync("user123")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(currentRoles);
        roleManagerMock.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(true);

        userManagerMock.Setup(x => x.RemoveFromRolesAsync(user, currentRoles))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.AddToRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new ChangeUserRoleRequest
        {
            UserId = "user123",
            NewRole = "Admin"
        };

        // Act
        var result = await service.ChangeUserRole(request, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Role changed to 'Admin' successfully.", result.Message);
    }

    [Fact]
    public async Task ChangeUserRole_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();

        userManagerMock.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityUser?)null);

        var service = new AccountService(userManagerMock.Object, roleManagerMock.Object);
        var context = CreateTestServerCallContext();

        var request = new ChangeUserRoleRequest
        {
            UserId = "nonexistent",
            NewRole = "Admin"
        };

        // Act
        var result = await service.ChangeUserRole(request, context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User not found.", result.Message);
    }
}
