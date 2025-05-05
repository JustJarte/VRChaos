using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Fusion.Addons.ConnectionManagerAddon
{ 
    /**
     * 
     * Handles:
     * - connection launch (either with room name or matchmaking session properties)
     * - user representation spawn on connection
     **/
#if XRSHARED_ADDON_AVAILABLE
    public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks, Fusion.XR.Shared.IUserSpawner
#else
    public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
#endif
    {
        [System.Flags]
        public enum ConnectionCriterias
        {
            RoomName = 1,
            SessionProperties = 2
        }

        [System.Serializable]
        public struct StringSessionProperty
        {
            public string propertyName;
            public string value;
        }

        [Header("Room configuration")]
        public GameMode gameMode = GameMode.Shared;
        public string roomName = "SampleFusion";
        public bool connectOnStart = true;
        [Tooltip("Set it to 0 to use the DefaultPlayers value, from the Global NetworkProjectConfig (simulation section)")]
        public int playerCount = 0;
        [SerializeField] private GameSettingsSO gameSettings;

        [Header("Room selection criteria")]
        public ConnectionCriterias connectionCriterias = ConnectionCriterias.RoomName;
        [Tooltip("If connectionCriterias include SessionProperties, additionalSessionProperties (editable in the inspector) will be added to sessionProperties")]
        public List<StringSessionProperty> additionalSessionProperties = new List<StringSessionProperty>();
        public Dictionary<string, SessionProperty> sessionProperties;

        [Header("Fusion settings")]
        [Tooltip("Fusion runner. Automatically created if not set")]
        public NetworkRunner runner;
        public INetworkSceneManager sceneManager;

        [Header("Local user spawner")]
        public List<NetworkObject> userPrefabs = new List<NetworkObject>();
        public NetworkObject userPrefab;

#region IUserSpawner
        public NetworkObject UserPrefab { 
            get => userPrefab;
            set => userPrefab = value;
        }
#endregion

        [Header("Event")]
        public UnityEvent onWillConnect = new UnityEvent();

        [Header("Info")]
        public List<StringSessionProperty> actualSessionProperties = new List<StringSessionProperty>();

        // Dictionary of spawned user prefabs, to store them on the server for host topology, and destroy them on disconnection (for shared topology, use Network Objects's "Destroy When State Authority Leaves" option)
        private Dictionary<PlayerRef, NetworkObject> _spawnedUsers = new Dictionary<PlayerRef, NetworkObject>();
        private PlayerRef thisPlayer;

        bool ShouldConnectWithRoomName => (connectionCriterias & ConnectionManager.ConnectionCriterias.RoomName) != 0;
        bool ShouldConnectWithSessionProperties => (connectionCriterias & ConnectionManager.ConnectionCriterias.SessionProperties) != 0;

        private void Awake()
        {
            // Check if a runner exist on the same game object
            if (runner == null) runner = FindObjectOfType<NetworkRunner>();
            //if (runner == null) runner = GetComponent<NetworkRunner>();

            // Create the Fusion runner and let it know that we will be providing user input
            if (runner == null) runner = gameObject.AddComponent<NetworkRunner>();
            runner.ProvideInput = true;

            //Keep an eye on this to make sure it works correctly or needs to be moved
            //var cryptidCharIndex = (int)gameSettings.selectedCryptidCharacter;
            //userPrefab = userPrefabs[cryptidCharIndex];
            UserPrefab = userPrefab;
        }

        private async void Start()
        {
            if (runner && new List<NetworkRunner>(GetComponentsInParent<NetworkRunner>()).Contains(runner) == false)
            {
                // The connectionManager is not in the hierarchy of the runner, so it has not been automatically subscribed to its callbacks
                runner.AddCallbacks(this);
            }
            // Launch the connection at start
            if (connectOnStart) await Connect();
        }

        Dictionary<string, SessionProperty> AllConnectionSessionProperties
        {
            get
            {
                var propDict = new Dictionary<string, SessionProperty>();
                actualSessionProperties = new List<StringSessionProperty>();
                if (sessionProperties != null)
                {
                    foreach (var prop in sessionProperties)
                    {
                        propDict.Add(prop.Key, prop.Value);
                        actualSessionProperties.Add(new StringSessionProperty { propertyName = prop.Key, value = prop.Value });
                    }
                }
                if (additionalSessionProperties != null)
                {
                    foreach (var additionalProperty in additionalSessionProperties)
                    {
                        propDict[additionalProperty.propertyName] = additionalProperty.value;
                        actualSessionProperties.Add(additionalProperty);
                    }

                }
                return propDict;
            }
        }

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

        public void TestGoingBack()
        {
            StartCoroutine(GoBackAfterDelay());
        }

        private IEnumerator GoBackAfterDelay()
        {
            yield return new WaitForSeconds(10.0f);

            yield return new WaitForSeconds(2.0f);

            runner.Shutdown();
        }

        public async Task Connect()
        {
            // Create the scene manager if it does not exist
            if (sceneManager == null) sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            if (onWillConnect != null) onWillConnect.Invoke();

            if (runner.IsServer)
            {
                if (sessionProperties == null)
                {
                    sessionProperties = new Dictionary<string, SessionProperty>();
                }

                sessionProperties["matchStartTime"] = (SessionProperty)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            // Start or join (depends on gamemode) a session with a specific name
            var args = new StartGameArgs()
            {
                GameMode = gameMode,
                Scene = CurrentSceneInfo(),
                SceneManager = sceneManager
            };
            // Connection criteria
            if (ShouldConnectWithRoomName)
            {
                args.SessionName = roomName;
            }
            if (ShouldConnectWithSessionProperties)
            {
                args.SessionProperties = AllConnectionSessionProperties;
            }
            // Room details
            if (playerCount > 0)
            {
                args.PlayerCount = playerCount;
            }

            await runner.StartGame(args);

            string prop = "";
            if (runner.SessionInfo.Properties != null && runner.SessionInfo.Properties.Count > 0)
            {
                prop = "SessionProperties: ";
                foreach (var p in runner.SessionInfo.Properties) prop += $" ({p.Key}={p.Value.PropertyValue}) ";
            }
            Debug.Log($"Session info: Room name {runner.SessionInfo.Name}. Region: {runner.SessionInfo.Region}. {prop}");
            if ((connectionCriterias & ConnectionManager.ConnectionCriterias.RoomName) == 0)
            {
                roomName = runner.SessionInfo.Name;
            }
        }

#region Player spawn
        public void OnPlayerJoinedSharedMode(NetworkRunner runner, PlayerRef player)
        {
            if (player == runner.LocalPlayer && userPrefab != null)
            {
                // Spawn the user prefab for the local user
                NetworkObject networkPlayerObject = runner.Spawn(userPrefab, position: new Vector3(0.0f, 1.783f, 0.0f), rotation: transform.rotation, player, (runner, obj) => {
                });

                thisPlayer = player;
            }
        }

        public void OnPlayerJoinedHostMode(NetworkRunner runner, PlayerRef player)
        {
            // The user's prefab has to be spawned by the host
            if (runner.IsServer && userPrefab != null)
            {
                Debug.Log($"OnPlayerJoined. PlayerId: {player.PlayerId}");
                // We make sure to give the input authority to the connecting player for their user's object
                NetworkObject networkPlayerObject = runner.Spawn(userPrefab, position: new Vector3(UnityEngine.Random.Range(-3.0f, 3.0f), 1.783f, UnityEngine.Random.Range(-3.0f, 3.0f)), rotation: transform.rotation, inputAuthority: player, (runner, obj) => {
                });

                // Keep track of the player avatars so we can remove it when they disconnect
                _spawnedUsers.Add(player, networkPlayerObject);

                thisPlayer = player;
            }
        }

        // Despawn the user object upon disconnection
        public void OnPlayerLeftHostMode(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log("I'm trying to disconnect the player from the game.");

            // Find and remove the players avatar (only the host would have stored the spawned game object)
            if (_spawnedUsers.TryGetValue(player, out NetworkObject networkObject))
            {
                runner.Despawn(networkObject);
                _spawnedUsers.Remove(player);
                Debug.Log("Succeeded removing the player");
            }
        }

#endregion

#region INetworkRunnerCallbacks
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.Topology == Topologies.ClientServer)
            {
                long matchStartTime = 0;

                if (runner.SessionInfo.Properties.TryGetValue("matchStartTime", out SessionProperty startTimeProperty))
                {
                    matchStartTime = (long)startTimeProperty.PropertyValue;
                }

                if (matchStartTime > 0.0f)
                {
                    var elapsedSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - matchStartTime;

                    if (elapsedSeconds > 300)
                    {
                        Debug.LogWarning($"Player {player.PlayerId} tried to join after match started. Disconnecting...");

                        if (player == runner.LocalPlayer)
                        {
                            runner.Shutdown();
                            return;
                        }
                    }
                }

                OnPlayerJoinedHostMode(runner, player);
            }
            else
            {
                OnPlayerJoinedSharedMode(runner, player);
            }
        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
            if (runner.Topology == Topologies.ClientServer)
            {
                OnPlayerLeftHostMode(runner, player);
            }
        }
#endregion

#region INetworkRunnerCallbacks (debug log only)
        public void OnConnectedToServer(NetworkRunner runner) {
            Debug.Log("OnConnectedToServer");

        }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log("Shutdown: " + shutdownReason);
            ReturnToLobby();
        }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.Log("OnDisconnectedFromServer: "+ reason);
            ReturnToLobby();
        }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {
            Debug.Log("OnConnectFailed: " + reason);
        }
#endregion

        private void ReturnToLobby()
        {
            if (SceneManager.GetActiveScene().name != "LobbyScene")
            {
                Debug.Log("[ConnectionManager] Returning to lobby scene...");

                StartCoroutine(FadeBackToLobby());
            }
        }

        private IEnumerator FadeBackToLobby()
        {
            yield return new WaitForSeconds(2.0f);

            SceneManager.LoadScene(0);
        }

#region Unused INetworkRunnerCallbacks 

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, ArraySegment<byte> data){}
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, float progress){}
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
#endregion
    }

}
