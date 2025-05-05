using Fusion.XR.Shared.Rig;
using UnityEngine;

public enum TurnMode
{
    Default,
    SnapTurn,
    Smooth
}

[CreateAssetMenu(menuName = "VRChaos/Option Settings")]
public class OptionSettingsSO : ScriptableObject
{
    public bool vignetteOnFallEnabled = false;
    public bool playerRightHanded = true;
    public float movementSensitivity = 1.0f; //Default is 1.0
    public TurnMode turnModeSelected;

    private const string VIGNETTE_PREF_KEY = "VignetteToggleChoice";
    private const string PLAYER_HANDEDNESS_KEY = "PlayerHandedChoice";
    private const string MOVEMENT_SENSITIVITY_KEY = "MovementSensitivityValue";
    private const string TURN_MODE_KEY = "TurnModeChoice";

    public void SetVignetteOnFall(bool b)
    {
        vignetteOnFallEnabled = b;
    }

    public void SetPrimaryHand(bool b)
    {
        playerRightHanded = b;
    }

    public void SetMovementSensitivity(float value)
    {
        movementSensitivity = value;
    }

    public void SetTurnModeSelection(int index)
    {
        turnModeSelected = (TurnMode)index;
    }

    public void SaveOptionSettings()
    {
        PlayerPrefs.SetInt(VIGNETTE_PREF_KEY, (vignetteOnFallEnabled ? 1 : 0));
        PlayerPrefs.SetInt(PLAYER_HANDEDNESS_KEY, (playerRightHanded ? 1 : 0));
        PlayerPrefs.SetFloat(MOVEMENT_SENSITIVITY_KEY, movementSensitivity);
        PlayerPrefs.SetInt(TURN_MODE_KEY, (int)turnModeSelected);

        PlayerPrefs.Save();
    }

    public void LoadSavedOptionSettings()
    {
        if (PlayerPrefs.HasKey(PLAYER_HANDEDNESS_KEY))
        {
            vignetteOnFallEnabled = (PlayerPrefs.GetInt(VIGNETTE_PREF_KEY) != 0);
            playerRightHanded = (PlayerPrefs.GetInt(PLAYER_HANDEDNESS_KEY) != 0);
            movementSensitivity = PlayerPrefs.GetFloat(MOVEMENT_SENSITIVITY_KEY);
            turnModeSelected = (TurnMode)PlayerPrefs.GetInt(TURN_MODE_KEY);
        }
    }

    public XRControllerInputDevice GetPrimaryHand(XRControllerInputDevice leftHand, XRControllerInputDevice rightHand)
    {
        return playerRightHanded ? rightHand : leftHand;
    }
}
