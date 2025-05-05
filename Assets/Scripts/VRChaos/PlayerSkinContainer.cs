using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "VRChaos/Player Skins Container")]
public class PlayerSkinContainer : ScriptableObject
{
    public List<PlayerSkin> playerSkins = new List<PlayerSkin>();

    public Dictionary<string, PlayerSkin> playerSkinDictionary = new Dictionary<string, PlayerSkin>();

    public void FillDictionary()
    {
        foreach (var skin in playerSkins)
        {
            playerSkinDictionary.Add(skin.skinName, skin);
        }
    }

    public PlayerSkin GetSpecificSkin(string skinName)
    {
        return playerSkinDictionary[skinName];
    }
}
