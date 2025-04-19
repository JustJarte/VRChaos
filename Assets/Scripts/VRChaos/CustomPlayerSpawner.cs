using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;


public class CustomPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks, Fusion.XR.Shared.IUserSpawner
{
    [SerializeField] private NetworkObject playerPrefab;

    private Dictionary<PlayerRef, NetworkObject> _spawnedUsers = new Dictionary<PlayerRef, NetworkObject>();

    #region IUserSpawner
    public NetworkObject UserPrefab
    {
        get => playerPrefab;
        set => playerPrefab = value;
    }
    #endregion

    public void OnPlayerJoinedHostMode(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && playerPrefab != null)
        {
            var spawnPoint = GetRandomSpawn();

            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, position: spawnPoint, rotation: Quaternion.identity, inputAuthority: player, (runner2, obj) => { });

            _spawnedUsers.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerJoinedSharedMode(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer && playerPrefab != null)
        {
            var spawnPoint = GetRandomSpawn();

            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, position: spawnPoint, rotation: Quaternion.identity, player, (runner2, obj) => { });
        }
    }

    public void OnPlayerLeftHostMode(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedUsers.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedUsers.Remove(player);
        }
    }

    private Vector3 GetRandomSpawn()
    {
        return new Vector3(UnityEngine.Random.Range(-2.0f, 2.0f), 2.0f, UnityEngine.Random.Range(-2.0f, 2.0f));
    }

    #region INetworkRunnerCallbacks

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.Topology == Topologies.ClientServer)
        {
            OnPlayerJoinedHostMode(runner, player);
        }
        else
        {
            OnPlayerJoinedSharedMode(runner, player);
        }
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.Topology == Topologies.ClientServer)
        {
            OnPlayerLeftHostMode(runner, player);
        }
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason netDisconnectReason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    #endregion
}
