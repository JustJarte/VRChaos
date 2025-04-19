using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Prefabs and Settings")]
    public List<NetworkObject> cryptidPlayerPrefabs;
    public GameSettingsSO gameSettings;

    // Dictionary of spawned user prefabs, to store them on the server for host topology, and destroy them on disconnection (for shared topology, use Network Objects's "Destroy When State Authority Leaves" option)
    private Dictionary<PlayerRef, NetworkObject> _spawnedUsers = new Dictionary<PlayerRef, NetworkObject>();

    public virtual NetworkSceneInfo CurrentSceneInfo()
    {
        var activeScene = SceneManager.GetActiveScene();
        SceneRef sceneRef = default;

        if (activeScene.buildIndex < 0 || activeScene.buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("Current scene is not part of the build settings");
        }
        else
        {
            sceneRef = SceneRef.FromIndex(activeScene.buildIndex);
        }

        var sceneInfo = new NetworkSceneInfo();
        if (sceneRef.IsValid)
        {
            sceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Single);
        }
        return sceneInfo;
    }

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

    public void OnPlayerJoinedHostMode(NetworkRunner runner, PlayerRef player)
    {
        // The user's prefab has to be spawned by the host
        if (runner.IsServer)
        {
            Debug.Log($"OnPlayerJoined. PlayerId: {player.PlayerId}");

            int cryptidCharIndex = (int)gameSettings.selectedCryptidCharacter;

            if (cryptidCharIndex < 0 || cryptidCharIndex >= cryptidPlayerPrefabs.Count)
            {
                Debug.LogError($"Invalid cryptid character index: {cryptidCharIndex}");
                return;
            }

            NetworkObject playerPrefab = cryptidPlayerPrefabs[cryptidCharIndex];

            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, inputAuthority: player);

            var typeHandler = networkPlayerObject.GetComponent<CryptidTypeHandler>();

            if (typeHandler != null)
            {
                typeHandler.CryptidCharacterType = gameSettings.selectedCryptidCharacter;
            }

            _spawnedUsers.Add(player, networkPlayerObject);
            //_spawnedUsers[player] = networkPlayerObject;

            Debug.Log($"[Server] Spawned player with Cryptid {gameSettings.selectedCryptidCharacter} for {player}");
            //_spawnedUsers.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerJoinedSharedMode(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            int cryptidCharIndex = (int)gameSettings.selectedCryptidCharacter;

            if (cryptidCharIndex < 0 || cryptidCharIndex >= cryptidPlayerPrefabs.Count)
            {
                Debug.LogError($"Invalid cryptid character index: {cryptidCharIndex}");
                return;
            }

            NetworkObject playerPrefab = cryptidPlayerPrefabs[cryptidCharIndex];

            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);

            var typeHandler = networkPlayerObject.GetComponent<CryptidTypeHandler>();

            if (typeHandler != null)
            {
                typeHandler.CryptidCharacterType = gameSettings.selectedCryptidCharacter;
            }

            Debug.Log($"[Shared] Spawned local player Cryptid {gameSettings.selectedCryptidCharacter} for {player}");
        }
    }

    // Despawn the user object upon disconnection
    public void OnPlayerLeftHostMode(NetworkRunner runner, PlayerRef player)
    {
        // Find and remove the players avatar (only the host would have stored the spawned game object)
        if (_spawnedUsers.TryGetValue(player, out var networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedUsers.Remove(player);
        }
    }

    #region Unused Required INetworkCallbacks
    public void OnConnectedToServer(NetworkRunner runner) => Debug.Log("Connected to Server");
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => Debug.Log($"Disconnected: {reason}");
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) => Debug.Log($"Connect failed: {reason}");
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) => Debug.Log($"Runner Shutdown: {shutdownReason}");

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}
