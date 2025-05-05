using Fusion;
using UnityEngine;
using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine.SceneManagement;
using System.Collections;

public class TagModeManager : NetworkBehaviour
{
    public static TagModeManager Instance { get; private set; }

    [SerializeField] private float timeBeforeStart = 10.0f;

    [Networked] public TickTimer StartTimer { get; set; }
    [Networked] public bool GameStarted { get; set; } = false;
    [Networked, Capacity(10)] public NetworkLinkedList<PlayerRef> ActivePlayers => default;
    [Networked, Capacity(10)] public NetworkLinkedList<PlayerRef> RabidPlayers => default;
    public bool TimerStarted { get; set; } = false;

    private NetworkRunner runner;
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
            StartTagMode();
        }
    }

    public void RegisterPlayer(PlayerRef player)
    {
        if (!ActivePlayers.Contains(player))
        {
            Debug.Log($"Registered player {player.PlayerId}!");
            ActivePlayers.Add(player);
        }

        Debug.Log($"Player count in registered players now: {ActivePlayers.Count}");
    }

    private void StartTagMode()
    {
        GameStarted = true;

        if (ActivePlayers.Count == 0) return;

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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_DisplayRabidMessage(PlayerRef rabidPlayer)
    {
        foreach (var player in FindObjectsOfType<Player>())
        {
            if (player.Object.InputAuthority == rabidPlayer)
            {
                if (player.HasInputAuthority)
                {
                    ScreenFader.Instance?.ShowMessage("You've been infected!", 2.0f);
                }
                else
                {
                    string name = player.namePlateHandler.name;
                    ScreenFader.Instance?.ShowMessage($"{name} has been infected!", 2.0f);
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_DisplayGlobalMessage(string message)
    {
        ScreenFader.Instance?.ShowMessage(message, 2.0f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_DisplayGlobalCountdown(float countdown)
    {
        if (countdown > 10.0f)
        {
            ScreenFader.Instance?.SetCountdown(countdown);
        }
        else
        {
            ScreenFader.Instance?.SetFinalCountdown(countdown);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_EndGameAndReturnAllUsersToLobby(PlayerRef lastPlayer)
    {
        ScreenFader.Instance?.ShowMessage($"Match is over! We all became rabid! {lastPlayer.PlayerId} was the last Cryptid! Congratulations!");

        StartCoroutine(EndGameAndReturnToLobby());
    }

    private IEnumerator CountdownRoutine(float countdownTime)
    {
        Debug.Log("Started the timer until game starts...");
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

        //ScreenFader.Instance?.HideCountdown();
    }

    private IEnumerator EndGameAndReturnToLobby()
    {
        if (gameEnded) yield break;
        gameEnded = true;

        yield return new WaitForSeconds(2.0f);

        ScreenFader.Instance?.FadeToBlack();

        yield return new WaitForSeconds(3.0f);

        ActivePlayers.Clear();
        RabidPlayers.Clear();

        NetworkRunnerHandler.Instance.EndSessionAndReturnToLobby();

        //Destroy(this); //Does this need to be called?
    }
}
