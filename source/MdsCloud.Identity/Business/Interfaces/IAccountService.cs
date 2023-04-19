using MdsCloud.Identity.Business.DTOs;

namespace MdsCloud.Identity.Business.Interfaces;

public interface IAccountService
{
    long RegisterNewAccount(ArgsWithTrace<AccountRegistrationArgs> registrationRequest);
}
