using MdsCloud.Identity.Core.DTOs;

namespace MdsCloud.Identity.Core.Interfaces;

public interface IAccountService
{
    long RegisterNewAccount(ArgsWithTrace<AccountRegistrationArgs> registrationRequest);
}
