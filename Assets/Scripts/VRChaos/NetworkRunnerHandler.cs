using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEngine.Events;
using System.Collections;
using Random = UnityEngine.Random;

// An Instanced class that handles all NetworkRunner calls and logic, including connecting, spawning, leaving, etc.
public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunnerHandler Instance { get; private set; }

    [System.Serializable]
    public struct StringSessionProperty
    {
        public string propertyName;
        public string value;
    }

    [Header("Prefabs and Settings")]
    public GameMode gameConnectionMode = GameMode.AutoHostOrClient;
    public Dictionary<string, SessionProperty> sessionProperties;
    public List<StringSessionProperty> additionalSessionProperties = new List<StringSessionProperty>();
    public List<StringSessionProperty> actualSessionProperties = new List<StringSessionProperty>();
    public List<NetworkObject> cryptidPlayerPrefabs;
    [Tooltip("Only for debug multiplayer testing. Remove when done.")] [SerializeField] private NetworkObject botMultiplayerPrefab;
    [SerializeField] [HideInInspector] private NetworkRunner runner;
    public GameSettingsSO gameSettings;
    public SpawnLocationsSO spawnLocationCollection;
    public bool connectOnStart = false;

    [Header("Audio Clips")] [Space(10.0f)]
    public AudioClip connectedToServer;
    public AudioClip disconnectedFromServer;
    public AudioClip shutdown;
    public AudioClip connectFailed;
    public AudioClip localUserSpawned;
    public AudioClip playerJoined;
    public AudioClip playerLeft;

    [Header("Event")] [Space(10.0f)]
    public UnityEvent onWillConnect = new UnityEvent();

    public NetworkObject UserPrefab { get { return userPrefab; } set { userPrefab = value; } }
    public NetworkRunner InstanceRunner { get { return runner; } }

    // Dictionary of spawned user prefabs, to store them on the server for host topology, and destroy them on disconnection (for shared topology, use Network Objects's "Destroy When State Authority Leaves" option).
    private Dictionary<PlayerRef, NetworkObject> _spawnedUsers = new Dictionary<PlayerRef, NetworkObject>();
    private Vector3 startingPos;
    private Quaternion startingRot;
    private AudioSource audioSource;
    private NetworkSceneManagerDefault sceneManager;
    private NetworkObject userPrefab;

    private int playerCounter = 0; // Used for debugging stuff in the Unity Editor, not actually used in a build.

    // Create Instance.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // Only works if in the Editor, allows me to create bots for general testing.
    private void Update()
    {
#if UNITY_EDITOR
        if (runner != null && runner.IsServer && Input.GetKeyDown(KeyCode.P))
        {
            SpawnBotForMultiplayerTesting();
        }
#endif
    }

    // Dictionary of all SessionProperties
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

    // Gets the current scene's info, as this object follows the player from scene to scene.
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

    // Handles connecting to an online session of the selected game mode by starting or joining a session with a specific name. Connect is called whenever the Player loads into a game mode scene and its Manager calls Connect on Start.
    public async Task Connect()
    {
        if (runner == null)
        {
            var runnerGO = new GameObject("NetworkRunnerObject");
            runner = runnerGO.AddComponent<NetworkRunner>();

            runner.ProvideInput = true;

            if (runner)
            {
                runner.AddCallbacks(this);
            }

            if (!runner.TryGetComponent(out NetworkSceneManagerDefault sceneMgr))
            {
                sceneMgr = runnerGO.AddComponent<NetworkSceneManagerDefault>();
            }

            sceneManager = sceneMgr;
        }

        if (onWillConnect != null) onWillConnect.Invoke();

        if (runner.IsServer)
        {
            if (sessionProperties == null)
            {
                sessionProperties = new Dictionary<string, SessionProperty>();
            }

            sessionProperties["matchStartTime"] = (SessionProperty)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        Debug.Log($"[{(Application.isEditor ? "Editor" : "Build")}] GameMode Selected: {gameConnectionMode}");

        gameSettings.CreateStartGameArgs(gameConnectionMode, CurrentSceneInfo(), AllConnectionSessionProperties, sceneManager);

        var args = gameSettings.GetStartArgs();

        await runner.StartGame(args);
    }

    // Handles when a player joins a session.
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        audioSource.PlayOneShot(playerJoined);

        // If a player tries to join a match after a certain amount of time has passed, they get kicked back to the Lobby and are unable to join; this is to prevent players joining a match that has already been going on for some time and disrupting flow or logic. 
        // Likely will change this to prevent after the countdown amount has hit 0 instead.
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

    // Handles when a player leaves a session.
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        audioSource.PlayOneShot(playerLeft);

        if (runner.Topology == Topologies.ClientServer)
        {
            OnPlayerLeftHostMode(runner, player);
        }
    }

    // If a player joined and its a hosting mode type, this logic is performed. We spawn the correct player prefab based on their selected Cryptid character in the Lobby, then we get the location to spawn them at via our spawn location collection and the
    // current game mode, and then once that player is spawned, we add them to the current spawned users list.
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

            string positionStringName = "";

            if (gameSettings.selectedGamePlayMode == GamePlayMode.Tag)
            {
                positionStringName = "FreePlayModeStartingPos";// "TagModeStartingCavePos";
            }
            else if (gameSettings.selectedGamePlayMode == GamePlayMode.Battle)
            {
                positionStringName = "BattleModeStartingPos";
            }
            else if (gameSettings.selectedGamePlayMode == GamePlayMode.Decryptid)
            {
                positionStringName = "DecryptidModeStartingPos";
            }
            else if (gameSettings.selectedGamePlayMode == GamePlayMode.FreePlay)
            {
                positionStringName = "FreePlayModeStartingPos";
            }

            for (int i = 0; i < spawnLocationCollection.spawnLocations.Count; i++)
            {
                if (spawnLocationCollection.spawnLocations[i].positionName == positionStringName)
                {
                    startingPos = spawnLocationCollection.spawnLocations[i].position;
                    startingRot = spawnLocationCollection.spawnLocations[i].rotation;
                }
            }

            var offset = Vector3.up * 1.5f;

            /*if (playerCounter > 0)
            {
                networkPlayerObject = runner.Spawn(botMultiplayerPrefab, position: startingPos + offset, rotation: startingRot, inputAuthority: player);

                playerCounter++;
            }
            else
            {
                networkPlayerObject = runner.Spawn(playerPrefab, position: startingPos + offset, rotation: startingRot, inputAuthority: player);

                playerCounter++;
            }*/

            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, position: startingPos + offset, rotation: startingRot, inputAuthority: player);

            _spawnedUsers.Add(player, networkPlayerObject);

            Debug.Log($"[Server] Spawned player with Cryptid {gameSettings.selectedCryptidCharacter} for {player}");
        }
    }

    // If a player joined and its a sharing mode type, this logic is performed. We spawn the correct player prefab based on their selected Cryptid character in the Lobby, then we get the location to spawn them at via our spawn location collection and the
    // current game mode, and then once that player is spawned, we add them to the current spawned users list.
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

            string positionStringName = "";

            if (gameSettings.selectedGamePlayMode == GamePlayMode.Tag)
            {
                positionStringName = "FreePlayModeStartingPos";// "TagModeStartingCavePos";
            }
            else if (gameSettings.selectedGamePlayMode == GamePlayMode.Battle)
            {
                positionStringName = "BattleModeStartingPos";
            }
            else if (gameSettings.selectedGamePlayMode == GamePlayMode.Decryptid)
            {
                positionStringName = "DecryptidModeStartingPos";
            }
            else if (gameSettings.selectedGamePlayMode == GamePlayMode.FreePlay)
            {
                positionStringName = "FreePlayModeStartingPos";
            }

            for (int i = 0; i < spawnLocationCollection.spawnLocations.Count; i++)
            {
                if (spawnLocationCollection.spawnLocations[i].positionName == positionStringName)
                {
                    startingPos = spawnLocationCollection.spawnLocations[i].position;
                    startingRot = spawnLocationCollection.spawnLocations[i].rotation;
                }
            }

            var offset = Vector3.up * 1.5f;

            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, position: startingPos + offset, rotation: startingRot, inputAuthority: player);

            Debug.Log($"[Shared] Spawned local player Cryptid {gameSettings.selectedCryptidCharacter} for {player}");
        }
    }

    // Despawn the user object upon disconnection and then remove them from the spawned users list.
    public void OnPlayerLeftHostMode(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedUsers.TryGetValue(player, out var networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedUsers.Remove(player);
        }
    }

    // Handles spawning a bot for dev testing.
    public void SpawnBotForMultiplayerTesting()
    {
        if (!runner.IsServer)
        {
            Debug.LogWarning("Only the server should spawn bots!");
            return;
        }

        NetworkObject botPrefab = botMultiplayerPrefab;
        Vector3 spawnPos = new Vector3(Random.Range(-10.0f, 10.0f), 2.0f, Random.Range(-10.0f, 10.0f));
        Quaternion spawnRot = Quaternion.identity;

        NetworkObject botObject = runner.Spawn(botPrefab, spawnPos, spawnRot, inputAuthority: null);

        var bot = botObject.GetComponent<BotController>();

        if (bot != null)
        {
            bot.InitializeAsBot();
        }

        Debug.Log("[Fusion] Bot player has been spawned in.");
    }

    // Handles connecting to server.
    public void OnConnectedToServer(NetworkRunner runner)
    {
        audioSource.PlayOneShot(connectedToServer);

        Debug.Log("OnConnectedToServer");
    }
    // Handles server shutdown.
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        audioSource.PlayOneShot(shutdown);

        Debug.Log("Shutdown: " + shutdownReason);
        ReturnToGameLobby();
    }
    // Handles when a player is disconnected from server.
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        audioSource.PlayOneShot(disconnectedFromServer);

        Debug.Log("OnDisconnectedFromServer: " + reason);
        ReturnToGameLobby();
    }
    // Handles when a player cannot connect to a room.
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        audioSource.PlayOneShot(connectFailed);

        Debug.Log("OnConnectFailed: " + reason);
    }

    // Ends the current online session and returns all users to their lobby.
    public void EndSessionAndReturnToLobby()
    {
        if (runner != null)
        {
            foreach (var kvp in _spawnedUsers)
            {
                if (kvp.Value != null)
                {
                    runner.Despawn(kvp.Value);
                }
            }

            _spawnedUsers.Clear();

            runner.Shutdown();
            Destroy(runner.gameObject);
            runner = null;
        }
    }

    // Returns the user to their lobby when the server is shutdown or they disconnect from the server by calling a Coroutine to handle the shift.
    private void ReturnToGameLobby()
    {
        if (SceneManager.GetActiveScene().name != "StartingLobby")
        {
            Debug.Log("[Handler] Returning to lobby scene...");

            StartCoroutine(FadeBackToLobby());
        }
    }

    // Fades player's view, loads back to the lobby scene, and then resets the HUD and player's view.
    private IEnumerator FadeBackToLobby()
    {
        MultipurposeHUD.Instance?.FadeToBlack();

        yield return new WaitForSeconds(3.0f);

        SceneManager.LoadScene(0);

        yield return new WaitForSeconds(2.0f);

        MultipurposeHUD.Instance.ResetHUD();
    }

#region Unused Required INetworkCallbacks
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
