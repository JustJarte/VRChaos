using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameSettingsController : MonoBehaviour
{
    [SerializeField] private GameSettingsSO gameSettings;
    [SerializeField] private PreviewCharacter characterPreviewController;

    [Space(10.0f)] public UnityEvent onGameStarted;

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
        characterPreviewController.PreviewSelectedRig(gameSettings.selectedCryptidCharacter.ToString());
    }
}
