using Fusion;
using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleModeManager : NetworkBehaviour
{
    public static BattleModeManager Instance { get; private set; }

    [SerializeField] private float timeBeforeStart = 10.0f;

    [Networked] public TickTimer StartTimer { get; set; }
    [Networked] [HideInInspector] public bool GameStarted { get; set; } = false;
    [Networked, Capacity(10)] public NetworkLinkedList<PlayerRef> ActivePlayers => default;
    [Networked, Capacity(10)] private NetworkDictionary<PlayerRef, int> PlayerLives => default;
    [Networked, Capacity(10)] private NetworkDictionary<PlayerRef, CryptidCharacterType> PlayerTypes => default;
    [Networked, Capacity(10)] private NetworkLinkedList<PlayerRef> EliminatedPlayers => default;

    public bool TimerStarted { get; set; } = false;

    private bool gameEnded = false;

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

    public void StartTimerNow()
    {
        TimerStarted = true;
        StartTimer = TickTimer.CreateFromSeconds(Runner, timeBeforeStart);
        StartCoroutine(CountdownRoutine(timeBeforeStart));
    }

    public override void FixedUpdateNetwork()
    {
        if (GameStarted)
        {
            return;
        }

        if (StartTimer.Expired(Runner))
        {
            StartBattleMode();
        }
    }

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
        GameStarted = true;

        if (ActivePlayers.Count == 0) return;

        Debug.Log("Battle mode has started!");
    }

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

            if (EliminatedPlayers.Contains(player))
            {
                continue;
            }

            var type = PlayerTypes[player];
            bool isLastOfItsKind = aliveCount.ContainsKey(type) && aliveCount[type] == 1;

            var p = FindLastOfKindPlayer(player);

            if (p != null)
            {
                p.HasBuff = true;
            }
        }
    }

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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_DisplayGlobalMessage(string message)
    {
        ScreenFader.Instance?.ShowMessage(message, 2.0f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_EndGameAndReturnAllUsersToLobby()
    {
        StartCoroutine(EndGameAndReturnToLobby());
    }

    private IEnumerator EndGameAndReturnToLobby()
    {
        if (gameEnded) yield break;
        gameEnded = true;

        yield return new WaitForSeconds(2.0f);

        //ScreenFader.FadeToBlack();

        yield return new WaitForSeconds(2.0f);

        ActivePlayers.Clear();
        PlayerLives.Clear();
        PlayerTypes.Clear();
        EliminatedPlayers.Clear();

        NetworkRunnerHandler.Instance.EndSessionAndReturnToLobby();

        //Destroy(this);
    }

    private IEnumerator CountdownRoutine(float countdownTime)
    {
        Debug.Log("Started the timer until game starts...");
        float timer = countdownTime;

        while (timer > 10.0f)
        {
            ScreenFader.Instance?.SetCountdown(timer);
            timer -= 1.0f;

            yield return new WaitForSeconds(1.0f);
        }

        ScreenFader.Instance?.SetCountdown(timer);

        yield return new WaitForSeconds(timer);

        Rpc_DisplayGlobalMessage("Match has started! Go!");

        //ScreenFader.Instance?.HideCountdown();
    }
}
