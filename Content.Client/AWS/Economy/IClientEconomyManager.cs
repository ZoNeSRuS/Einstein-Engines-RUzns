using Content.Shared.AWS.Economy;

namespace Content.Client.AWS.Economy;

public interface IClientEconomyManager : ISharedEconomyManager
{
    /// <summary>
    /// Raised when the account list is received from the server.
    /// </summary>
    event EventHandler AccountUpdateReceived;

    void AccountUpdateRequest();
}
