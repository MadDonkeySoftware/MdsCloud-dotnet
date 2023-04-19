using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Business.DTOs;
using MdsCloud.Identity.Business.Exceptions;
using MdsCloud.Identity.Business.Interfaces;
using MdsCloud.Identity.Business.Utils;
using MdsCloud.Identity.Domain;
using NHibernate;

namespace MdsCloud.Identity.Business.Services;

public class UserService : IUserService
{
    private readonly ILogger _logger;
    private readonly ISessionFactory _sessionFactory;

    public UserService(ILogger logger, ISessionFactory sessionFactory)
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
    }

    public void UpdateUserData(ArgsWithTrace<UpdateUserDataArgs> args)
    {
        if (args.Data.RequestingUserJwt == null)
        {
            throw new ArgumentException("RequestingUserJwt cannot be null");
        }

        using var session = _sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();
        var shouldUpdate = false;
        var jwt = args.Data.RequestingUserJwt;

        var userId = jwt.Claims.First(c => c.Type == "userId").Value;
        var user = session.Query<User>().First(u => u.Id == userId);

        if (
            args.Data.OldPassword != null
            && args.Data.NewPassword != null
            && args.Data.OldPassword != args.Data.NewPassword
        )
        {
            if (!PasswordHasher.Verify(args.Data.OldPassword, user.Password))
            {
                throw new InvalidPasswordException();
            }

            user.Password = PasswordHasher.Hash(args.Data.NewPassword);
            shouldUpdate = true;
        }

        if (args.Data.Email != null)
        {
            user.Email = args.Data.Email;
            shouldUpdate = true;
        }

        if (args.Data.FriendlyName != null)
        {
            user.FriendlyName = args.Data.FriendlyName;
            shouldUpdate = true;
        }

        if (!shouldUpdate)
        {
            throw new ArgumentException("Found no action to perform");
        }

        user.LastModified = DateTime.UtcNow;
        session.SaveOrUpdate(user);
        transaction.Commit();
        _logger.LogWithMetadata(
            LogLevel.Debug,
            "Successfully updated user",
            args.MdsTraceId,
            new Dictionary<string, dynamic> { { "userId", userId }, }
        );
    }
}
