using Fusion.XR.Shared.Rig;
using UnityEngine;

// TurnMode defines the turn mode style in-game. GorillaTag typically utilizes a full physics based system, so turning is done in real life, however, I have seen in some gameplay videos
// that additional turn modes have been added, so I also added them to allow for user choice. Default = physics based turning, SnapTurn = snap rotation by 45 degrees, and Smooth = a smooth full rotation as long as
// the player holds the left or right control stick.
public enum TurnMode
{
    Default,
    SnapTurn,
    Smooth
}

// ScriptableObject that controls and holds saved values for options set in the options menu, including: handedness, a vignette-on-fall effect, movement sensitivity, and turn mode. Options can be
// saved and then loaded upon the game loading in to set their last saved settings.
[CreateAssetMenu(menuName = "VRChaos/Option Settings")]
public class OptionSettingsSO : ScriptableObject
{
    public bool vignetteOnFallEnabled = false;
    public bool playerRightHanded = true;
    public float movementSensitivity = 1.0f; 
    public TurnMode turnModeSelected;

    private const string VIGNETTE_PREF_KEY = "VignetteToggleChoice";
    private const string PLAYER_HANDEDNESS_KEY = "PlayerHandedChoice";
    private const string MOVEMENT_SENSITIVITY_KEY = "MovementSensitivityValue";
    private const string TURN_MODE_KEY = "TurnModeChoice";

    // Turns on a vignette effect that fades in when the user is falling from a far height to reduce motion sickness.
    public void SetVignetteOnFall(bool b)
    {
        vignetteOnFallEnabled = b;
    }

    // Determines the player's primary hand for gameplay; based on their choice, certain actions will be adjusted for that primary hand.
    public void SetPrimaryHand(bool b)
    {
        playerRightHanded = b;
    }

    // Movement sensitivity will adjust 
    public void SetMovementSensitivity(float value)
    {
        movementSensitivity = value;
    }

    // Sets the player's preferred turn mode style. 
    public void SetTurnModeSelection(int index)
    {
        turnModeSelected = (TurnMode)index;
    }

    // Saves all current set options.
    public void SaveOptionSettings()
    {
        PlayerPrefs.SetInt(VIGNETTE_PREF_KEY, (vignetteOnFallEnabled ? 1 : 0));
        PlayerPrefs.SetInt(PLAYER_HANDEDNESS_KEY, (playerRightHanded ? 1 : 0));
        PlayerPrefs.SetFloat(MOVEMENT_SENSITIVITY_KEY, movementSensitivity);
        PlayerPrefs.SetInt(TURN_MODE_KEY, (int)turnModeSelected);

        PlayerPrefs.Save();
    }

    // Loads all current saved options if they exist.
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

    // Returns the Input Device of the primary hand, utilized for quick conditional checks.
    public XRControllerInputDevice GetPrimaryHand(XRControllerInputDevice leftHand, XRControllerInputDevice rightHand)
    {
        return playerRightHanded ? rightHand : leftHand;
    }
}
