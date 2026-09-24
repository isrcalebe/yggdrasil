using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Modules.Identity.Contracts.v1.Accounts;
using yggdrasil.Modules.Identity.Domain;

namespace yggdrasil.Integration.Tests.Modules.Identity;

public sealed class AccountRegistrationTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private const string password = "correct-horse-battery-9A";

    private static readonly Uri accounts = new("/api/v1/identity/accounts", UriKind.Relative);

    public static TheoryData<string> InvalidEmails =>
    [
        "",
        "not-an-email",
        new string('a', 245) + "@yggdrasil.cc",
    ];

    public static TheoryData<string> WeakPasswords =>
    [
        "",
        "Sh0rt-a",
        "no-digits-Here",
        "all-lowercase-9",
        "ALL-UPPERCASE-9",
        "NoSymbols9",
    ];

    [Fact]
    public async Task RegisteringCreatesAccountWithHashedPassword()
    {
        using var response = await Client.PostAsJsonAsync(accounts, new RegisterAccountCommand("player@yggdrasil.cc", password), CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterAccountResponse>(CancellationToken);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.AccountId);

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();

        var account = await userManager.FindByIdAsync(body.AccountId.ToString());
        Assert.NotNull(account);
        Assert.Equal("player@yggdrasil.cc", account.Email);
        Assert.Equal("player@yggdrasil.cc", account.UserName);
        Assert.NotEqual(password, account.PasswordHash);
        Assert.True(await userManager.CheckPasswordAsync(account, password));
    }

    [Fact]
    public async Task DuplicateEmailIgnoringCaseIsConflict()
    {
        using var first = await Client.PostAsJsonAsync(accounts, new RegisterAccountCommand("player@yggdrasil.cc", password), CancellationToken);
        using var second = await Client.PostAsJsonAsync(accounts, new RegisterAccountCommand("Player@Yggdrasil.cc", password), CancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        Assert.Contains("identity.email_taken", await second.Content.ReadAsStringAsync(CancellationToken), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(InvalidEmails))]
    public async Task InvalidEmailIsValidationProblem(string email)
    {
        var errors = await registerExpectingValidationProblem(new RegisterAccountCommand(email, password));

        Assert.Contains("email", errors.Keys);
        Assert.DoesNotContain("password", errors.Keys);
    }

    [Theory]
    [MemberData(nameof(WeakPasswords))]
    public async Task WeakPasswordIsValidationProblem(string weakPassword)
    {
        var errors = await registerExpectingValidationProblem(new RegisterAccountCommand("player@yggdrasil.cc", weakPassword));

        Assert.Contains("password", errors.Keys);
        Assert.DoesNotContain("email", errors.Keys);
    }

    [Fact]
    public async Task ValidationRunsWhenAnotherModuleSendsTheCommand()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await mediator.Send(new RegisterAccountCommand("not-an-email", "weak"), CancellationToken));

        Assert.Contains(exception.Errors, static failure => failure.PropertyName == nameof(RegisterAccountCommand.Email));
        Assert.Contains(exception.Errors, static failure => failure.PropertyName == nameof(RegisterAccountCommand.Password));
        Assert.Equal(0, await countAccounts());
    }

    [Fact]
    public async Task MissingFieldsAreValidationProblem()
    {
        using var response = await Client.PostAsync(accounts, JsonContent.Create(new { }), CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("email", problem!.Errors.Keys);
        Assert.Contains("password", problem!.Errors.Keys);
    }

    /// <summary>Posts a registration that must fail validation, and checks that nothing was persisted.</summary>
    private async Task<IDictionary<string, string[]>> registerExpectingValidationProblem(RegisterAccountCommand command)
    {
        using var response = await Client.PostAsJsonAsync(accounts, command, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal(0, await countAccounts());

        return problem.Errors;
    }

    private async Task<int> countAccounts()
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<UserManager<Account>>().Users.CountAsync(CancellationToken);
    }
}
