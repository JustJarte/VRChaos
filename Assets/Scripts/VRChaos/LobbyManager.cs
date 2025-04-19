using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class LobbyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSettingsSO gameSettings;
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private NetworkRunner runnerPrefab;

    [Header("Game References")]
    [SerializeField] private List<GameObject> characterPrefabs;
    [SerializeField] private List<string> modeSceneNames;

    private GameObject currentCharacterPreviewed;
    private NetworkRunner instancedNetworkRunner;

    private void Awake()
    {
        var networkRunner = FindObjectOfType<NetworkRunner>();

        if (networkRunner != null)
        {
            instancedNetworkRunner = networkRunner;
        }
        else
        {
            instancedNetworkRunner = Instantiate(runnerPrefab);
            DontDestroyOnLoad(instancedNetworkRunner.gameObject); // Persist through scene load

            instancedNetworkRunner.ProvideInput = true;
        }
    }

    private void Start()
    {
        //UpdateCryptidCharacterPreview(gameSettings.selectedCryptidCharacter);
    }

    public void SelectCryptidCharacter(int index)
    {
        gameSettings.selectedCryptidCharacter = (CryptidCharacterType)index;
        //UpdateCryptidCharacterPreview(gameSettings.selectedCryptidCharacter);
    }

    public void DebugLog(string displayText)
    {
        Debug.Log("I'm showing that clicking on buttons is working: " + displayText);
    }

    public void SelectGameMode(int index)
    {
        gameSettings.selectedGamePlayMode = (GamePlayMode)index;

        //Do some fancy fade here before loading the game mode scene
        StartGame();
    }

    public void StartGame()
    {
        StartCoroutine(StartGameSequence());
    }

    //Maybe retool for skins here
    private void UpdateCryptidCharacterPreview(CryptidCharacterType selectedCharacter)
    {

    }

    private IEnumerator StartGameSequence()
    {
        yield return FadeToBlack();

        var sceneToLoad = gameSettings.GetSceneNameForSelectedMode();

        var sceneInfo = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneToLoad).buildIndex);

        int allowedPlayerCount = gameSettings.GetModePlayerLimit();

        var startGameArgs = new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            Scene = sceneInfo,
            SessionName = gameSettings.selectedGamePlayMode.ToString(),
            PlayerCount = allowedPlayerCount,
            SceneManager = instancedNetworkRunner.GetComponent<NetworkSceneManagerDefault>()
        }; 

        yield return instancedNetworkRunner.StartGame(startGameArgs);
    }

    private IEnumerator FadeToBlack()
    {
        fadeCanvas.gameObject.SetActive(true);

        while (fadeCanvas.alpha < 1.0f)
        {
            fadeCanvas.alpha += Time.deltaTime * 1.5f;
            yield return null;
        }
    }
}
