using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Basic manager class for the starting lobby; sets the selected character and selected mode when chosen and saves them to the GameSettingsSO. Methods are made public so that they can be called directly from a UI Button so that they are performed when clicked.
public class LobbyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSettingsSO gameSettings;
    [SerializeField] private GameObject errorMessage;
    [SerializeField] private GameObject player;

    private GameObject currentCharacterPreviewed;
    private bool selectedCharacter = false;

    // If a preferred character is saved in PlayerPrefs, this is made true on Awake so that the player can just jump straight into a game mode without having to reselect a character just to put this flag to TRUE.
    private void Awake()
    {
        if (gameSettings.selectedCryptidCharacter != CryptidCharacterType.Default)
        {
            selectedCharacter = true;
        }
    }

    // Method to select a character, called from the selectable Cryptid pictures in the lobby. When they are clicked, saves that character.
    public void SelectCryptidCharacter(int index)
    {
        gameSettings.selectedCryptidCharacter = (CryptidCharacterType)index;

        selectedCharacter = true;
    }

    // Method to select a game mode, called from the selectable buttons on the dry erase board in the lobby. When they are clicked, saves that selected game mode to jump into when the Start Button is pressed.
    public void SelectGameMode(int index)
    {
        if (errorMessage.activeSelf)
        {
            errorMessage.SetActive(false);
        }

        gameSettings.selectedGamePlayMode = (GamePlayMode)index;
    }

    // Method to start a selected game mode via Coroutine, called from the selectable Start Button on the dry erase board in the lobby. Loads the scene of the game mode the player selected.
    public void StartGame()
    {
        if (gameSettings.selectedGamePlayMode != GamePlayMode.NoSelection && selectedCharacter)
        {
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

    //Maybe retool for choosing alt-skins here
    private void UpdateCryptidCharacterPreview(CryptidCharacterType selectedCharacter) { }

    // Fades the player's view and loads into the new scene.
    private IEnumerator StartGameSequence()
    {
        MultipurposeHUD.Instance?.FadeToBlack();

        yield return new WaitForSeconds(2.0f);

        SceneManager.LoadScene((int)gameSettings.selectedGamePlayMode + 1);
    }
}
