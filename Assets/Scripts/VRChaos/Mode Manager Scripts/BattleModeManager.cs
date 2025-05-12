using Fusion;
using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles the Battle Mode for VRChaos; Battle Mode is essentially a free for all, where players are equipped with a tranquilizer crossbow and are able to shoot other players. Once a player has been hit 3 times, they are Eliminated and become 'ghosts'. 
// 'Ghosts' can still participate, however they cannot eliminate another player. Their weapon still shoots, but instead of taking a life, it simply reduces the player's movement speed for a few seconds. In addition to the unique debuff mechanic, there's also
// a buff mechanic, wherein the game tracks all types of Cryptids and if a player is the remaining one of its type (i.e. Bigfoot, Mothman, etc.), they get a "Last of Its Kind" buff that increases movement speed and jump to give them a slight edge. 
// Once a final player remains, the game ends by congratulating the final player and returning all players to the starting lobby. 

public class BattleModeManager : NetworkBehaviour
{
    public static BattleModeManager Instance { get; private set; }

    [SerializeField] private float timeBeforeStart = 180.0f;
    [SerializeField] private GameSettingsSO gameSettings;

    public bool TimerStarted { get; set; } = false;
    [Networked] public TickTimer StartTimer { get; set; }
    [Networked] [HideInInspector] public bool GameStarted { get; set; } = false;

    [Networked, Capacity(10)] [HideInInspector] public NetworkLinkedList<PlayerRef> ActivePlayers => default;
    [Networked, Capacity(10)] private NetworkDictionary<PlayerRef, int> PlayerLives => default;
    [Networked, Capacity(10)] private NetworkDictionary<PlayerRef, CryptidCharacterType> PlayerTypes => default;
    [Networked, Capacity(10)] private NetworkLinkedList<PlayerRef> EliminatedPlayers => default;

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
                StartBattleMode();
            }
        }
    }

    // Registers all players that join the game mode, saves them to an additional collection with their max lives, and also saves their Cryptid Type for Battle Mode-specific mechanics.
    public void RegisterPlayer(PlayerRef player, CryptidCharacterType type)
    {
        if (!PlayerLives.ContainsKey(player))
        {
            ActivePlayers.Add(player);
            PlayerLives.Add(player, 3);
            PlayerTypes.Add(player, type);
        }
    }

    private void StartBattleMode()
    {
        if (ActivePlayers.Count == 0) return;

        GameStarted = true;

        Debug.Log("Battle mode has started!");
    }

    // Track when a player has been hit in the game; remove a life from their total, and if it's 0, eliminate that player. Additionally runs the condition check for the remaining Players to apply any "Last of Their Kind" buffs. Also if the total eliminated players
    // is 1 less than the total of active players there were, that means 1 player remains and thus they are the last remaining player and the game has been won.
    public void PlayerHit(PlayerRef victim, PlayerRef attacker)
    {
        if (!PlayerLives.ContainsKey(victim) || EliminatedPlayers.Contains(victim))
        {
            return;
        }

        int lives = PlayerLives[victim] - 1;
        PlayerLives.Set(victim, lives);

        if (PlayerLives[victim] <= 0)
        {
            if (!EliminatedPlayers.Contains(victim))
            {
                EliminatedPlayers.Add(victim);
            }

            Debug.Log($"Player {victim.PlayerId} has been eliminated!");
        }

        CheckForBattleModeBuff();

        if (EliminatedPlayers.Count == ActivePlayers.Count - 1)
        {
            CheckIfMatchIsOver();
        }
    }

    // The conditional check that checks if any remaining players are the 'last of their kind', and give them a bonus to movement speed and jump, if so.
    private void CheckForBattleModeBuff()
    {
        Dictionary<CryptidCharacterType, int> aliveCount = new Dictionary<CryptidCharacterType, int>();

        foreach (var kvp in PlayerLives)
        {
            if (!EliminatedPlayers.Contains(kvp.Key))
            {
                var type = PlayerTypes[kvp.Key];

                if (!aliveCount.ContainsKey(type))
                {
                    aliveCount[type] = 0;
                }

                aliveCount[type]++;
            }
        }

        foreach (var set in PlayerLives)
        {
            var player = set.Key;
            var lives = set.Value;

            if (EliminatedPlayers.Contains(player)) continue;

            var type = PlayerTypes[player];
            bool isLastOfItsKind = aliveCount.ContainsKey(type) && aliveCount[type] == 1;

            var p = FindLastOfKindPlayer(player);

            if (p != null)
            {
                p.HasBuff = true;
            }
        }
    }

    // Finds the player with Input Authority and if true, return that Player or return null if not.
    private Player FindLastOfKindPlayer(PlayerRef player)
    {
        foreach (var p in FindObjectsOfType<Player>())
        {
            if (p.Object.InputAuthority == player)
            {
                return p;
            }
        }

        return null;
    }

    // Only the host can check this condition: does final checks to make sure only 1 Player remains and if so, announce their victory, and then Rpc to return all current game mode players to their respective lobby.
    private void CheckIfMatchIsOver()
    {
        if (Object.HasStateAuthority)
        {
            int alive = 0;
            PlayerRef lastAlive = default;

            foreach (var set in PlayerLives)
            {
                var player = set.Key;
                var lives = set.Value;

                if (!EliminatedPlayers.Contains(player))
                {
                    alive++;
                    lastAlive = player;
                }
            }

            if (alive == 1)
            {
                Debug.Log($"Player {lastAlive.PlayerId} wins the game and is the last Cryptid standing!");

                Rpc_EndGameAndReturnAllUsersToLobby();
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
        PlayerLives.Clear();
        PlayerTypes.Clear();
        EliminatedPlayers.Clear();

        NetworkRunnerHandler.Instance.EndSessionAndReturnToLobby();
    }
}
