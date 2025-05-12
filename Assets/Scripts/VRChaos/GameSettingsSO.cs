using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Game mode enum to control the state of the game based on the user's pick.
public enum GamePlayMode
{
    Decryptid,
    Battle,
    FreePlay,
    Tag,
    NoSelection
}

// ScriptableObject that sets and controls general game settings, such as the character the user picked, the mode they picked to play, and responsible for creating the StartGameArgs to pass to the NetworkRunnerHandler for each player.
[CreateAssetMenu(menuName = "VRChaos/Game Settings")]
public class GameSettingsSO : ScriptableObject
{
    public CryptidCharacterType selectedCryptidCharacter = CryptidCharacterType.Default;
    public GamePlayMode selectedGamePlayMode = GamePlayMode.NoSelection;

    public float smoothTurningSpeed = 90.0f;
    public float snapTurnAngle = 45.0f;
    public float snapTurnCooldown = 0.3f;

    private const string CRYPTID_PREF_KEY = "LastCryptid";

    private StartGameArgs currentStartArgs;
    private GameMode currentGameMode;
    private NetworkSceneInfo currentSceneInfo;
    private string currentSessionName;
    private Dictionary<string, SessionProperty> currentSessionProperties;
    private NetworkSceneManagerDefault currentManager;

    // Resets general game settings and then resets the user to their preferred Cryptid character in the Lobby.
    public void ResetGameSettings()
    {
        if (PlayerPrefs.HasKey(CRYPTID_PREF_KEY))
        {
            selectedCryptidCharacter = (CryptidCharacterType)PlayerPrefs.GetInt(CRYPTID_PREF_KEY);
        }
        else
        {
            selectedCryptidCharacter = CryptidCharacterType.Default;
        }

        selectedGamePlayMode = GamePlayMode.NoSelection;

        currentSessionName = "";

        if (currentSessionProperties != null)
        {
            currentSessionProperties.Clear();
        }

        currentManager = null;
    }

    // Saves a player's preferred Cryptid when selected.
    public void SaveSelectedCryptid()
    {
        PlayerPrefs.SetInt(CRYPTID_PREF_KEY, (int)selectedCryptidCharacter);
        PlayerPrefs.Save();
    }

    // Creates the StartGameArgs to eventually be passed to NetworkRunnerHandler utilizing the scene, mode, player count limit, session properties, manager, connection mode, and session name, which is then used to Connect to that mode's online game.
    public void CreateStartGameArgs(GameMode currentGameConnectionMode, NetworkSceneInfo sceneInfo, Dictionary<string, SessionProperty> sessionProperties, NetworkSceneManagerDefault manager)
    {
        currentSceneInfo = sceneInfo;
        currentGameMode = currentGameConnectionMode;
        currentSessionName = selectedGamePlayMode.ToString();
        currentSessionProperties = sessionProperties;
        currentManager = manager;

        currentStartArgs = new StartGameArgs
        {
            GameMode = currentGameMode,
            Scene = currentSceneInfo,
            SessionName = currentSessionName,
            PlayerCount = GetModePlayerLimit(),
            SessionProperties = currentSessionProperties,
            SceneManager = currentManager         
        };
    }

    // Returns the created StartGameArgs to be used.
    public StartGameArgs GetStartArgs()
    {
        if (currentStartArgs.SessionName == selectedGamePlayMode.ToString())
        {
            return currentStartArgs;
        }

        return default;
    }

    // Returns a string of the scene name corresponding to its scene in the Build Index based on the GamePlayMode selected. Currently hard-coded, but could be better utilized in the future.
    public string GetSceneNameForSelectedMode()
    {
        switch (selectedGamePlayMode)
        {
            case GamePlayMode.Decryptid:
                return "DecryptidMode_AlphaMap";
            case GamePlayMode.Battle:
                return "BattleMode_AlphaMap";
            case GamePlayMode.FreePlay:
                return "FreePlayMode_AlphaMap";
            case GamePlayMode.Tag:
                return "TagMode_AlphaMap";
            default:
                return "";
        }
    }

    // Returns an int that acts as the PlayerCount for StartGameArgs based on the GamePlayMode selected. Values are just a bit random at the moment, but could be better set in the future on full release.
    public int GetModePlayerLimit()
    {
        switch (selectedGamePlayMode)
        {
            case GamePlayMode.Decryptid:
                return 15;
            case GamePlayMode.Battle:
                return 10;
            case GamePlayMode.FreePlay:
                return 20;
            case GamePlayMode.Tag:
                return 20;
            default:
                return 1;
        }
    }
}
