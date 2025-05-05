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
    [SerializeField] private NetworkRunner runner;
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

    // Dictionary of spawned user prefabs, to store them on the server for host topology, and destroy them on disconnection (for shared topology, use Network Objects's "Destroy When State Authority Leaves" option)
    private Dictionary<PlayerRef, NetworkObject> _spawnedUsers = new Dictionary<PlayerRef, NetworkObject>();
    private Vector3 startingPos;
    private Quaternion startingRot;
    private AudioSource audioSource;
    private NetworkSceneManagerDefault sceneManager;
    private NetworkObject userPrefab;

    private int playerCounter = 0;

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

    private void Update()
    {
#if UNITY_EDITOR
        if (runner != null && runner.IsServer && Input.GetKeyDown(KeyCode.P))
        {
            SpawnBotForMultiplayerTesting();
        }
#endif
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

#if PARRELSYNC
        gameConnectionMode = GameMode.AutoHostOrClient;
#else
        gameConnectionMode = GameMode.AutoHostOrClient;
#endif

        Debug.Log($"[{(Application.isEditor ? "Editor" : "Build")}] GameMode Selected: {gameConnectionMode}");

        gameSettings.CreateStartGameArgs(gameConnectionMode, CurrentSceneInfo(), AllConnectionSessionProperties, sceneManager);

        // Start or join (depends on gamemode) a session with a specific name
        var args = gameSettings.GetStartArgs();

        await runner.StartGame(args);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        audioSource.PlayOneShot(playerJoined);

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
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        audioSource.PlayOneShot(playerLeft);

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
            NetworkObject networkPlayerObject;

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

            networkPlayerObject = runner.Spawn(playerPrefab, position: startingPos + offset, rotation: startingRot, inputAuthority: player);

            var typeHandler = networkPlayerObject.GetComponent<CryptidTypeHandler>();

            if (typeHandler != null)
            {
                typeHandler.CryptidCharacterType = gameSettings.selectedCryptidCharacter;
            }

            _spawnedUsers.Add(player, networkPlayerObject);

            Debug.Log($"[Server] Spawned player with Cryptid {gameSettings.selectedCryptidCharacter} for {player}");
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

            NetworkObject networkPlayerObject = runner.Spawn(botMultiplayerPrefab, Vector3.zero, Quaternion.identity, player);

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

    public void SpawnBotForMultiplayerTesting()
    {
        if (!runner.IsServer)
        {
            Debug.LogWarning("Only the server should spawn bots!");
            return;
        }

        int cryptidCharIndex = (int)gameSettings.selectedCryptidCharacter;

        if (cryptidCharIndex < 0 || cryptidCharIndex >= cryptidPlayerPrefabs.Count)
        {
            Debug.LogError($"Invalid cryptid character index: {cryptidCharIndex}");
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

    public void OnConnectedToServer(NetworkRunner runner)
    {
        audioSource.PlayOneShot(connectedToServer);

        Debug.Log("OnConnectedToServer");
    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        audioSource.PlayOneShot(shutdown);

        Debug.Log("Shutdown: " + shutdownReason);
        ReturnToGameLobby();
    }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        audioSource.PlayOneShot(disconnectedFromServer);

        Debug.Log("OnDisconnectedFromServer: " + reason);
        ReturnToGameLobby();
    }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        audioSource.PlayOneShot(connectFailed);

        Debug.Log("OnConnectFailed: " + reason);
    }

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

    private void ReturnToGameLobby()
    {
        if (SceneManager.GetActiveScene().name != "StartingLobby")
        {
            Debug.Log("[Handler] Returning to lobby scene...");

            StartCoroutine(FadeBackToLobby());
        }
    }

    private IEnumerator FadeBackToLobby()
    {
        yield return new WaitForSeconds(2.0f);

        SceneManager.LoadScene(0);

        yield return new WaitForSeconds(2.0f);

        ScreenFader.Instance.ResetHUD();
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
