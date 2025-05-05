using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecryptidModeManager : NetworkBehaviour
{
    public static DecryptidModeManager Instance { get; private set; }

    [SerializeField] private NetworkRunner runner;

    private List<PlayerRef> activePlayers = new List<PlayerRef>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterPlayer(PlayerRef player)
    {
        if (!activePlayers.Contains(player))
        {
            activePlayers.Add(player);
        }
    }

    private IEnumerator ExitAfterFade()
    {
        ScreenFader.Instance?.FadeToBlack();

        yield return new WaitForSeconds(2.0f);

        runner.Shutdown();

        Destroy(this);
    }
}
