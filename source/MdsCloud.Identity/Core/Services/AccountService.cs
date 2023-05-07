using System.Transactions;
using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Core.DTOs;
using MdsCloud.Identity.Core.Exceptions;
using MdsCloud.Identity.Core.Interfaces;
using MdsCloud.Identity.Core.Model;
using MdsCloud.Identity.Presentation.Utils;

namespace MdsCloud.Identity.Core.Services;

public class AccountService : IAccountService
{
    private readonly ILogger _logger;
    private readonly IAccountRepository _accountRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISettings _settings;

    public AccountService(
        ILogger logger,
        IAccountRepository accountRepository,
        IUserRepository userRepository,
        ISettings settings
    )
    {
        _logger = logger;
        _accountRepository = accountRepository;
        _userRepository = userRepository;
        _settings = settings;
    }

    public long RegisterNewAccount(ArgsWithTrace<AccountRegistrationArgs> registrationRequest)
    {
        using var transaction = new TransactionScope();
        var accountExists = _accountRepository.AccountWithNameExists(
            registrationRequest.Data.AccountName
        );
        var userExists = _userRepository.UserWithNameExists(registrationRequest.Data.UserId);

        if (accountExists)
        {
            // TODO: include identifier
            throw new AccountExistsException();
        }

        if (userExists)
        {
            // TODO: include identifier
            throw new UserExistsException();
        }

        var bypassActivation = bool.Parse(_settings["MdsSettings:BypassUserActivation"] ?? "False");
        var newAccount = new Account
        {
            Name = registrationRequest.Data.AccountName,
            Created = DateTime.UtcNow,
            IsActive = bypassActivation,
        };
        var newUser = new User
        {
            Id = registrationRequest.Data.UserId,
            Created = DateTime.UtcNow,
            FriendlyName = registrationRequest.Data.FriendlyName,
            ActivationCode = bypassActivation ? null : RandomStringGenerator.GenerateString(32),
            IsPrimary = true,
            Email = registrationRequest.Data.Email,
            Password = PasswordHasher.Hash(registrationRequest.Data.Password),
            IsActive = bypassActivation,
        };

        _accountRepository.SaveAccount(newAccount);
        newUser.AccountId = newAccount.Id;
        _userRepository.SaveUser(newUser);
        transaction.Complete();

        _logger.LogWithMetadata(
            LogLevel.Debug,
            "Successfully registered new user",
            registrationRequest.MdsTraceId,
            new { AccountId = newAccount.Id, UserId = newUser.Id, }
        );

        return newAccount.Id;
    }
}
