using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Business.DTOs;
using MdsCloud.Identity.Business.Exceptions;
using MdsCloud.Identity.Business.Interfaces;
using MdsCloud.Identity.Business.Utils;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.Settings;
using Microsoft.IdentityModel.Tokens;
using NHibernate;

namespace MdsCloud.Identity.Business.Services;

public class TokenService : ITokenService
{
    private readonly ILogger _logger;
    private readonly ISessionFactory _sessionFactory;
    private readonly ISettings _settings;
    private readonly IFile _file;

    public TokenService(
        ILogger logger,
        ISessionFactory sessionFactory,
        ISettings settings,
        IFile file
    )
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
        _settings = settings;
        _file = file;
    }

    public string GenerateUserToken(ArgsWithTrace<GenerateUserTokenArgs> tokenRequest)
    {
        using var session = _sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();

        long parsedAccountId = long.TryParse(tokenRequest.Data.AccountId, out parsedAccountId)
            ? parsedAccountId
            : 0;
        var account = session.Query<Account>().FirstOrDefault(e => e.Id == parsedAccountId);
        var user = session.Query<User>().FirstOrDefault(e => e.Id == tokenRequest.Data.UserId);

        if (account == null)
        {
            throw new AccountDoesNotExistException();
        }

        if (!account.IsActive)
        {
            throw new AccountInactiveException();
        }

        if (user == null)
        {
            throw new UserDoesNotExistException();
        }

        if (!user.IsActive)
        {
            throw new UserInactiveException();
        }

        if (!PasswordHasher.Verify(tokenRequest.Data.Password, user.Password))
        {
            throw new InvalidPasswordException();
        }

        double parsedLifespanMinutes = double.TryParse(
            _settings["MdsSettings:JwtSettings:LifespanMinutes"],
            out parsedLifespanMinutes
        )
            ? parsedLifespanMinutes
            : 60d;

        var privateKeyBytes = _file.ReadAllText(_settings["MdsSettings:Secrets:PrivatePath"] ?? "");
        using var rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(
            privateKeyBytes,
            _settings["MdsSettings:Secrets:PrivatePassword"] ?? ""
        );
        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256
        )
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
        };
        var utcNow = DateTime.UtcNow;
        var tokenDescriptor = new JwtSecurityToken(
            new JwtHeader(signingCredentials),
            new JwtPayload(
                audience: _settings["MdsSettings:JwtSettings:Audience"],
                issuer: _settings["MdsSettings:JwtSettings:Issuer"],
                claims: new Claim[]
                {
                    new("accountId", account.Id.ToString()),
                    new("userId", user.Id),
                    new("friendlyName", user.FriendlyName),
                },
                notBefore: utcNow,
                issuedAt: utcNow,
                expires: utcNow.AddMinutes(parsedLifespanMinutes)
            )
        );
        user.LastActivity = DateTime.UtcNow;
        session.SaveOrUpdate(user);
        transaction.Commit();

        _logger.LogWithMetadata(
            LogLevel.Debug,
            $"Successfully authenticated user {user.Id}",
            tokenRequest.MdsTraceId
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    public string GenerateImpersonationToken(
        ArgsWithTrace<GenerateImpersonationTokenArgs> impersonationRequest
    )
    {
        if (impersonationRequest.Data.RequestingUserJwt == null)
        {
            throw new ArgumentException("RequestingUserJwt cannot be null");
        }

        using var session = _sessionFactory.OpenSession();
        var jwt = impersonationRequest.Data.RequestingUserJwt;
        var accountId = jwt.Claims.First(c => c.Type == "accountId").Value;
        var userId = jwt.Claims.First(c => c.Type == "userId").Value;
        long impersonatorAccountId = long.TryParse(accountId, out impersonatorAccountId)
            ? impersonatorAccountId
            : -1;

        if (impersonatorAccountId != 1 && accountId != impersonationRequest.Data.AccountId)
        {
            throw new InsufficientPrivilegeException();
        }

        long parsedAccountId = long.TryParse(
            impersonationRequest.Data.AccountId,
            out parsedAccountId
        )
            ? parsedAccountId
            : -1;
        var account = session.Query<Account>().FirstOrDefault(e => e.Id == parsedAccountId);
        if (account == null)
        {
            throw new AccountDoesNotExistException();
        }
        if (!account.IsActive)
        {
            throw new AccountInactiveException();
        }

        var userIdToSearchFor =
            impersonationRequest.Data.UserId ?? account.Users.First(u => u.IsPrimary).Id;
        var user = session.Query<User>().FirstOrDefault(e => e.Id == userIdToSearchFor);

        if (user == null)
        {
            throw new UserDoesNotExistException();
        }

        if (!user.IsActive)
        {
            throw new UserInactiveException();
        }

        double parsedLifespanMinutes = double.TryParse(
            _settings["MdsSettings:JwtSettings:LifespanMinutes"],
            out parsedLifespanMinutes
        )
            ? parsedLifespanMinutes
            : 60d;

        var privateKeyBytes = _file.ReadAllText(_settings["MdsSettings:Secrets:PrivatePath"] ?? "");
        using var rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(
            privateKeyBytes,
            _settings["MdsSettings:Secrets:PrivatePassword"] ?? ""
        );
        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256
        )
        {
            CryptoProviderFactory = new CryptoProviderFactory() { CacheSignatureProviders = false },
        };
        var utcNow = DateTime.UtcNow;
        var tokenDescriptor = new JwtSecurityToken(
            new JwtHeader(signingCredentials),
            new JwtPayload(
                audience: _settings["MdsSettings:JwtSettings:Audience"],
                issuer: _settings["MdsSettings:JwtSettings:Issuer"],
                claims: new Claim[]
                {
                    new("accountId", account.Id.ToString()),
                    new("userId", user.Id),
                    new("friendlyName", user.FriendlyName),
                    new("impersonatedBy", userId),
                    new("impersonatingFrom", accountId),
                },
                notBefore: utcNow,
                issuedAt: utcNow,
                expires: utcNow.AddMinutes(parsedLifespanMinutes)
            )
        );

        _logger.LogWithMetadata(
            LogLevel.Debug,
            "Impersonation successful",
            impersonationRequest.MdsTraceId,
            new
            {
                User = userId,
                Account = accountId,
                ImpersonatedUser = user.Id,
                ImpersonatedAccount = account.Id
            }
        );
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}
