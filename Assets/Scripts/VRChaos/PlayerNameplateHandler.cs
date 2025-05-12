using Fusion;
using TMPro;
using UnityEngine;

// Handles the Player's nameplate canvas; displays Player's name and updates it with any prefixes (such as "Rabid" or "Eliminated"), changes any colours if wanted but it's not really utilized for that right now.
public class PlayerNameplateHandler : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject teamColourIndicator; // Currently not in-use but put in for the future.
    [SerializeField] private Transform lookAtCameraTarget; // Currently not in-use but put in for the option later.

    private string originalDisplayName;

    // When Players are spawned, defines their name on their character. For the current Player, their name is displayed as "You", for others, you see their PlayerID.
    public override void Spawned()
    {
        if (nameText)
        {
            string displayName = Object.HasInputAuthority ? "You" : $"Player {Object.InputAuthority.PlayerId}";
            nameText.text = displayName;

            originalDisplayName = displayName;
        }

        var cryptidHandler = GetComponent<CryptidTypeHandler>();

        if (teamColourIndicator && cryptidHandler != null)
        {
            teamColourIndicator.GetComponent<Renderer>().material.color = GetColourForCryptid(cryptidHandler.CryptidCharacterType);
        }
    }

    // Updates display name to have a prefix if desired.
    public void UpdatePlayerPrefix(string appendText)
    {
        string currentName = nameText.text;
        nameText.text = appendText + currentName;
    }

    // Updates display name to be coloured if desired.
    public void UpdateNameDisplayColour(Color c)
    {
        nameText.color = c;
    }

    private void Update()
    {
        if (lookAtCameraTarget && Camera.main)
        {
            lookAtCameraTarget.rotation = Quaternion.LookRotation(lookAtCameraTarget.position - Camera.main.transform.position);
        }
    }

    // Can automatically colour the player's name based on their character choice, but isn't really utilized right now.
    private Color GetColourForCryptid(CryptidCharacterType characterType)
    {
        switch (characterType)
        {
            case CryptidCharacterType.Bigfoot:
                return new Color(0.5849056f, 0.3222791f, 0.0f);
            case CryptidCharacterType.Mothman:
                return Color.black;
            case CryptidCharacterType.Alien:
                return Color.cyan;
            case CryptidCharacterType.Frogman:
                return Color.green;
            default:
                return Color.gray;
        }
    }
}
