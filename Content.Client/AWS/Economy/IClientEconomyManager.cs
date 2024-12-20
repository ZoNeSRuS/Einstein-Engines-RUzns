using Content.Shared.AWS.Economy;

namespace Content.Client.AWS.Economy;

public interface IClientEconomyManager : ISharedEconomyManager
{
    void AccountUpdateRequest();
}
