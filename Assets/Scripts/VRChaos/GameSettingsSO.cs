using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GamePlayMode
{
    Decryptid,
    Battle,
    FreePlay,
    Tag
}

[CreateAssetMenu(menuName = "VRChaos/Game Settings")]
public class GameSettingsSO : ScriptableObject
{
    public CryptidCharacterType selectedCryptidCharacter = CryptidCharacterType.Default;
    public GamePlayMode selectedGamePlayMode = GamePlayMode.Decryptid;

    private const string CRYPTID_PREF_KEY = "LastCryptid";

    public void SaveSelectedCryptid()
    {
        PlayerPrefs.SetInt(CRYPTID_PREF_KEY, (int)selectedCryptidCharacter);
        PlayerPrefs.Save();
    }

    public void LoadLastSelectedCryptid()
    {
        if (PlayerPrefs.HasKey(CRYPTID_PREF_KEY))
        {
            selectedCryptidCharacter = (CryptidCharacterType)PlayerPrefs.GetInt(CRYPTID_PREF_KEY);
        }
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
