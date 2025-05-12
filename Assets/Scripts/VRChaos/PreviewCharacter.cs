using System.Collections.Generic;
using UnityEngine;

// Previews the selected character in the Lobby for the player.
public class PreviewCharacter : MonoBehaviour
{
    [SerializeField] private List<GameObject> previewRigs = new List<GameObject>();

    public void PreviewSelectedRig(string rigName)
    {
        foreach (var rig in previewRigs)
        {
            if (rig.name == rigName)
            {
                rig.SetActive(true);
            }
            else
            {
                rig.SetActive(false);
            }
        }
    }
}
