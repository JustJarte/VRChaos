using System.Collections.Generic;
using UnityEngine;

// ScriptableObject that acts as a collection of all current available Player Skins to be accessed and applied as necessary or desired, since it's generic enough for all uses. Currently is used to set the Rabid and Eliminated PlayerSkins for those respective Game
// Modes when the conditions are met. Can eventually be used to apply Alt Skins for players who unlock/buy them.
[CreateAssetMenu(menuName = "VRChaos/Player Skins Container")]
public class PlayerSkinContainer : ScriptableObject
{
    public List<PlayerSkin> playerSkins = new List<PlayerSkin>();

    public Dictionary<string, PlayerSkin> playerSkinDictionary = new Dictionary<string, PlayerSkin>();

    // Fills the collection so that you can get a specific skin by a string. Called when a Player component is instantiated.
    public void FillDictionary()
    {
        foreach (var skin in playerSkins)
        {
            playerSkinDictionary.Add(skin.skinName, skin);
        }
    }

    // Returns a specific skin by skinName string.
    public PlayerSkin GetSpecificSkin(string skinName)
    {
        return playerSkinDictionary[skinName];
    }
}
