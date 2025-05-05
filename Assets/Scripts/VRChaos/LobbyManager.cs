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
    [SerializeField] private GameObject errorMessage;
    [SerializeField] private GameObject player;

    private GameObject currentCharacterPreviewed;
    private bool selectedCharacter = false;

    private void Awake()
    {
        if (gameSettings.selectedCryptidCharacter != CryptidCharacterType.Default)
        {
            selectedCharacter = true;
        }
    }

    public void SelectCryptidCharacter(int index)
    {
        gameSettings.selectedCryptidCharacter = (CryptidCharacterType)index;

        selectedCharacter = true;
    }

    public void DebugLog(string displayText)
    {
        Debug.Log("I'm showing that clicking on buttons is working: " + displayText);
    }

    public void SelectGameMode(int index)
    {
        if (errorMessage.activeSelf)
        {
            errorMessage.SetActive(false);
        }

        gameSettings.selectedGamePlayMode = (GamePlayMode)index;
    }

    public void StartGame()
    {
        if (gameSettings.selectedGamePlayMode != GamePlayMode.NoSelection && selectedCharacter)
        {
            //Do some fancy fade here before loading the game mode scene
            StartCoroutine(StartGameSequence());
        }
        else if (gameSettings.selectedGamePlayMode == GamePlayMode.NoSelection && selectedCharacter)
        {
            Debug.Log("Mode has not been selected, please select a mode to start a game!");
            errorMessage.SetActive(true);
        }
        else if (!selectedCharacter)
        {
            Debug.Log("Please select a character; cannot start a game until a character is selected!");
        }
    }

    //Maybe retool for skins here
    private void UpdateCryptidCharacterPreview(CryptidCharacterType selectedCharacter)
    {

    }

    private IEnumerator StartGameSequence()
    {
        ScreenFader.Instance?.FadeToBlack();

        yield return new WaitForSeconds(2.0f);

        SceneManager.LoadScene((int)gameSettings.selectedGamePlayMode + 1);
    }
}
