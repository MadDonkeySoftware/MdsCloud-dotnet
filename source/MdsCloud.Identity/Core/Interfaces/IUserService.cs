using MdsCloud.Identity.Core.DTOs;

namespace MdsCloud.Identity.Core.Interfaces;

public interface IUserService
{
    public void UpdateUserData(ArgsWithTrace<UpdateUserDataArgs> args);
}
