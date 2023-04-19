using MdsCloud.Identity.Business.DTOs;

namespace MdsCloud.Identity.Business.Interfaces;

public interface IUserService
{
    public void UpdateUserData(ArgsWithTrace<UpdateUserDataArgs> args);
}
