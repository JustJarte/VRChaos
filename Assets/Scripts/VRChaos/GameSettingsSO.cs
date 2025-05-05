using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GamePlayMode
{
    Decryptid,
    Battle,
    FreePlay,
    Tag,
    NoSelection
}

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

    public void SaveSelectedCryptid()
    {
        PlayerPrefs.SetInt(CRYPTID_PREF_KEY, (int)selectedCryptidCharacter);
        PlayerPrefs.Save();
    }

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

    public StartGameArgs GetStartArgs()
    {
        return currentStartArgs;
    }

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
