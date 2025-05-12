using UnityEngine;

// Resets Game Settings to default upon starting game, clearing any populated Collections and additionally, if the player has a preferred character, it automatically sets up the default lobby rig to be that character. 
public class GameSettingsController : MonoBehaviour
{
    [SerializeField] private GameSettingsSO gameSettings;
    [SerializeField] private PreviewCharacter previewCharacterController;

    private void Awake()
    {
        gameSettings.ResetGameSettings();

        if (gameSettings.selectedCryptidCharacter != CryptidCharacterType.Default)
        {
            SetupPreferredCharacter();
        }
    }

    private void SetupPreferredCharacter()
    {
        previewCharacterController.PreviewSelectedRig(gameSettings.selectedCryptidCharacter.ToString());
    }
}
