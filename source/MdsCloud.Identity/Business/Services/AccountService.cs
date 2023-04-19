using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Business.DTOs;
using MdsCloud.Identity.Business.Exceptions;
using MdsCloud.Identity.Business.Interfaces;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.UI.Utils;
using NHibernate;

namespace MdsCloud.Identity.Business.Services;

public class AccountService : IAccountService
{
    private readonly ILogger _logger;
    private readonly ISessionFactory _sessionFactory;
    private readonly ISettings _settings;

    public AccountService(ILogger logger, ISessionFactory sessionFactory, ISettings settings)
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
        _settings = settings;
    }

    public long RegisterNewAccount(ArgsWithTrace<AccountRegistrationArgs> registrationRequest)
    {
        using var session = _sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();

        var accountExists = session
            .Query<Account>()
            .Any(e => e.Name == registrationRequest.Data.AccountName);
        var userExists = session.Query<User>().Any(e => e.Id == registrationRequest.Data.UserId);

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
            Account = newAccount,
            Created = DateTime.UtcNow,
            FriendlyName = registrationRequest.Data.FriendlyName,
            ActivationCode = bypassActivation ? null : RandomStringGenerator.GenerateString(32),
            IsPrimary = true,
            Email = registrationRequest.Data.Email,
            Password = PasswordHasher.Hash(registrationRequest.Data.Password),
            IsActive = bypassActivation,
        };

        newAccount.Users.Add(newUser);

        session.SaveOrUpdate(newAccount);
        session.SaveOrUpdate(newUser);
        transaction.Commit();

        _logger.LogWithMetadata(
            LogLevel.Debug,
            "Successfully registered new user",
            registrationRequest.MdsTraceId,
            new { AccountId = newAccount.Id, UserId = newUser.Id, }
        );

        return newAccount.Id;
    }
}
