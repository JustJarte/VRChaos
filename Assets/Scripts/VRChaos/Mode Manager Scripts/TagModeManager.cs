using Fusion;
using UnityEngine;
using GorillaLocomotion;
using System.Collections;

// Handles the Tag Mode for VRChaos; Tag Mode is essentially identical to the Infection Mode of normal GorillaTag. Once the match has started, a Player is chosen at random amongst the current active Players,
// and that Player becomes a 'Rabid' Cryptid. The goal is to chase other Players and spread the 'Rabid' status by tagging them. Once all Players have succumbed to the infection, the game match is over, we
// congratulate the last Player to become 'Rabid' and end the session and return all players back to their Lobby.
public class TagModeManager : NetworkBehaviour
{
    public static TagModeManager Instance { get; private set; }

    [SerializeField] private float timeBeforeStart = 180.0f;
    [SerializeField] private GameSettingsSO gameSettings;

    public bool TimerStarted { get; set; } = false;
    [Networked] public TickTimer StartTimer { get; set; }
    [Networked] [HideInInspector] public bool GameStarted { get; set; } = false;

    [Networked, Capacity(10)] [HideInInspector] public NetworkLinkedList<PlayerRef> ActivePlayers => default;
    [Networked, Capacity(10)] [HideInInspector] public NetworkLinkedList<PlayerRef> RabidPlayers => default;

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
                StartTagMode();
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

    // Once the game has officially started, we search through the list of active players and select a random one of them to become the first 'Rabid' Cryptid, flagging them as such and then sending out an Rpc message to everyone
    // letting them know who got the first infection.
    private void StartTagMode()
    {
        if (ActivePlayers.Count == 0) return;

        GameStarted = true;

        PlayerRef firstRabid = ActivePlayers[Random.Range(0, ActivePlayers.Count)];
        RabidPlayers.Add(firstRabid);

        Debug.Log($"Game started! First rabid cryptid: {firstRabid.PlayerId}!");

        foreach (var player in ActivePlayers)
        {
            var players = FindObjectsOfType<Player>();

            foreach (var p in players)
            {
                if (p.Object.InputAuthority == player)
                {
                    p.IsInfected = (player == firstRabid);

                    Rpc_DisplayRabidMessage(firstRabid);
                }
            }
        }
    }

    // Is called when a Player tags another while 'Rabid'. If that Player isn't already infected with the status, add them to the list and then send an Rpc message to all players to inform them of a new infected player.
    public void TagPlayer(PlayerRef newlyInfected)
    {
        if (!RabidPlayers.Contains(newlyInfected))
        {
            RabidPlayers.Add(newlyInfected);

            Rpc_DisplayRabidMessage(newlyInfected);

            Debug.Log($"Player {newlyInfected.PlayerId} was tagged and also infected! It continues to spread!");
        }

        CheckIfMatchIsOver();
    }

    // Only the host can check this condition: does final checks everyone has become 'Rabid', announce their victory, and then Rpc to return all current game mode players to their respective lobby.
    private void CheckIfMatchIsOver()
    {
        if (Object.HasStateAuthority)
        {
            if (RabidPlayers.Count == ActivePlayers.Count)
            {
                var lastPlayer = RabidPlayers[RabidPlayers.Count - 1];

                Debug.Log($"Match is over! Everyone's been infected! {lastPlayer.PlayerId} was the last Cryptid standing!");

                Rpc_EndGameAndReturnAllUsersToLobby(lastPlayer);
            }
        }
    }

    // Rpc to display a global message to all current players in this game mode with a specialized message for both the local player and everyone else.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_DisplayRabidMessage(PlayerRef rabidPlayer)
    {
        foreach (var player in FindObjectsOfType<Player>())
        {
            if (player.Object.InputAuthority == rabidPlayer)
            {
                if (player.HasInputAuthority)
                {
                    MultipurposeHUD.Instance?.ShowMessage("You've been infected!", 2.0f);
                }
                else
                {
                    string name = player.namePlateHandler.name;
                    MultipurposeHUD.Instance?.ShowMessage($"{name} has been infected!", 2.0f);
                }
            }
        }
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
    private void Rpc_EndGameAndReturnAllUsersToLobby(PlayerRef lastPlayer)
    {
        MultipurposeHUD.Instance?.ShowMessage($"Match is over! We all became rabid! {lastPlayer.PlayerId} was the last Cryptid! Congratulations!");

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
        RabidPlayers.Clear();

        NetworkRunnerHandler.Instance.EndSessionAndReturnToLobby();
    }
}
