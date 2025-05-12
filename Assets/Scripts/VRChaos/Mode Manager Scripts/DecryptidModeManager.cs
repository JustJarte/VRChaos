using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles the Decryptid Mode for VRChaos; Decryptid Mode is unique to my game in that it takes the Cryptid theme and creates a hunt-and-be-hunted kind of game loop where Players are encouraged to parkour around, search
// for Decryptid Camera items, which is a semi-rare drop in the game world. Once they obtain a Decryptid Camera, that Player is given a target to hunt for in the match and take a proper picture of them to prove their
// existence to the world and eliminate them from the game! The mechanics and ruleset for this mode are still in development, and thus it's not available to play yet. 
public class DecryptidModeManager : NetworkBehaviour
{
    public static DecryptidModeManager Instance { get; private set; }

    [SerializeField] private float timeBeforeStart = 180.0f;
    [SerializeField] private GameSettingsSO gameSettings;

    public bool TimerStarted { get; set; } = false;
    [Networked] public TickTimer StartTimer { get; set; }
    [Networked] [HideInInspector] public bool GameStarted { get; set; } = false;

    [Networked, Capacity(10)] [HideInInspector] public NetworkLinkedList<PlayerRef> ActivePlayers => default;
    [Networked, Capacity(10)] [HideInInspector] public NetworkLinkedList<PlayerRef> DecryptidPlayers => default;

    private Dictionary<PlayerRef, PlayerRef> playerTargets = new Dictionary<PlayerRef, PlayerRef>();
    private bool gameEnded = false;
    private bool startedCountdown = false;

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

    // Begins the game's timer to count down; default is 3 minutes. Once the timer hits 60 seconds, it begins an in-game countdown that all players can see to prepare them for the game to start. 
    public void StartTimerNow()
    {
        TimerStarted = true;
        StartTimer = TickTimer.CreateFromSeconds(Runner, timeBeforeStart);
    }

    public override void FixedUpdateNetwork()
    {
        if (GameStarted)
        {
            return;
        }

        if (TimerStarted)
        {
            // If we reach the cap of players and the timer is still above the 60 second threshold, go ahead and skip it down to 60 seconds to begin the countdown until game mode start.
            if (StartTimer.RemainingTime(Runner) > 60.0f && ActivePlayers.Count == gameSettings.GetModePlayerLimit())
            {
                StartTimer = TickTimer.CreateFromSeconds(Runner, 60.0f);
            }

            // Start in-game countdown from 60 seconds to game start.
            if (StartTimer.RemainingTime(Runner) <= 60.0f && !startedCountdown)
            {
                StartCoroutine(CountdownRoutine(timeBeforeStart));
            }

            if (StartTimer.Expired(Runner) && ActivePlayers.Count >= 2) // In a full release, this would be ActivePlayers.Count >= to a higher minimum for a match to start, where the value is the minimum number or higher of players, up to player limit.
            {
                StartDecryptidMode();
            }
        }
    }

    // Registers all players that join the game mode.
    public void RegisterPlayer(PlayerRef player)
    {
        if (!ActivePlayers.Contains(player))
        {
            ActivePlayers.Add(player);
        }
    }

    private void StartDecryptidMode() { } // Eventually will handle the logic to start Decryptid Mode. 

    private void CheckIfMatchIsOver() { } // Eventually will handle the logic to check if Decryptid Mode has met the criteria to end.

    // Assigns a Player target to be hunted in Decryptid Mode. Given their target, they hunt in the game to try and get a picture of that Player to prove to the world they exist!
    public void AssignTarget(PlayerRef hunter)
    {
        List<PlayerRef> otherPlayers = new List<PlayerRef>(ActivePlayers);
        otherPlayers.Remove(hunter);

        if (otherPlayers.Count == 0) return;

        var target = otherPlayers[Random.Range(0, otherPlayers.Count)];
        playerTargets[hunter] = target;

        Debug.Log($"Assigned {target} as target for {hunter}");
    }

    // This returns the target to be hunted for a specific Player when a Decryptid Camera item is picked up. 
    public PlayerRef GetTargetFor(PlayerRef player)
    {
        return playerTargets.ContainsKey(player) ? playerTargets[player] : default;
    }

    // Rpc to display a global message to all current players in this game mode.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_DisplayGlobalMessage(string message)
    {
        MultipurposeHUD.Instance?.ShowMessage(message, 2.0f);
    }

    // Rpc to display a global countdown until the match starts. If it's higher than 10 seconds, just continue a normal countdown, otherwise do the emphasized final countdown.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_DisplayGlobalCountdown(float countdown)
    {
        if (countdown > 10.0f)
        {
            MultipurposeHUD.Instance?.SetCountdown(countdown);
        }
        else
        {
            MultipurposeHUD.Instance?.SetFinalCountdown(countdown);
        }
    }

    // Rpc to return all current players to their starting lobby.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_EndGameAndReturnAllUsersToLobby()
    {
        StartCoroutine(EndGameAndReturnToLobby());
    }

    private IEnumerator CountdownRoutine(float countdownTime)
    {
        Debug.Log("Started the timer until game starts...");

        startedCountdown = true;
        float timer = countdownTime;

        while (timer > 10.0f)
        {
            Rpc_DisplayGlobalCountdown(timer);
            timer -= 1.0f;

            yield return new WaitForSeconds(1.0f);
        }

        Rpc_DisplayGlobalCountdown(timer);

        yield return new WaitForSeconds(timer);

        Rpc_DisplayGlobalMessage("Match has started! Go!");
    }

    private IEnumerator EndGameAndReturnToLobby()
    {
        if (gameEnded) yield break;
        gameEnded = true;

        ActivePlayers.Clear();
        DecryptidPlayers.Clear();
        playerTargets.Clear();

        NetworkRunnerHandler.Instance.EndSessionAndReturnToLobby();
    }
}
