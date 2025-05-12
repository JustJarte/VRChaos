using Fusion;
using System.Collections;
using UnityEngine;

// Handles the Free Play Mode for VRChaos; Free Play Mode is essentially just a server session of the game map where players can hang out with each other and just mess around, and eventually talk/interact in other ways.
// A player can return back to their Lobby from this session by hitting the return button. 
public class FreePlayModeManager : NetworkBehaviour
{
    public static FreePlayModeManager Instance { get; private set; }

    [Networked, Capacity(10)] public NetworkLinkedList<PlayerRef> ActivePlayers => default;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private async void Start()
    {
        await NetworkRunnerHandler.Instance.Connect();
    }

    // Registers all players that join the game mode.
    public void RegisterPlayer(PlayerRef player)
    {
        if (!ActivePlayers.Contains(player))
        {
            ActivePlayers.Add(player);
        }
    }

    // Sends a request to the host of the server that a player wants to leave Free Play Mode when the button is hit.
    public void ReturnToLobbyRequest(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log($"Player {player.PlayerId} requested to return to the lobby.");

        Rpc_ReturnToLobby(player);
    }

    // We find that player with an Rpc method after a player has signaled they want to leave.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ReturnToLobby(PlayerRef requestingPlayer)
    {
        if (Object.InputAuthority == requestingPlayer)
        {
            StartCoroutine(ExitFreePlayAndReturnToLobby());
        }
    }

    // Finally, in the Coroutine, we end the session for that player and send them back to their Lobby.
    private IEnumerator ExitFreePlayAndReturnToLobby()
    {
        yield return new WaitForSeconds(0.25f);

        NetworkRunnerHandler.Instance?.EndSessionAndReturnToLobby();
    }
}
