using Fusion;
using TMPro;
using UnityEngine;

public class PlayerNameplateHandler : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject teamColourIndicator; // optional: colored ring/icon? ask ChatGPT
    [SerializeField] private Transform lookAtCameraTarget; //optional: billboard to main camera

    public override void Spawned()
    {
        if (nameText)
        {
            string displayName = Object.HasInputAuthority ? "You" : $"Player {Object.InputAuthority.PlayerId}";
            nameText.text = displayName;
        }

        var cryptidHandler = GetComponent<CryptidTypeHandler>();

        if (teamColourIndicator && cryptidHandler != null)
        {
            teamColourIndicator.GetComponent<Renderer>().material.color = GetColourForCryptid(cryptidHandler.CryptidCharacterType);
        }
    }

    private void Update()
    {
        if (lookAtCameraTarget && Camera.main)
        {
            lookAtCameraTarget.rotation = Quaternion.LookRotation(lookAtCameraTarget.position - Camera.main.transform.position);
        }
    }

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
