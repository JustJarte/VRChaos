using System.Collections.Generic;
using UnityEngine;

// Serializable class that defines a Player Skin by taking a name for the skin, the Cryptid type its associated with (i.e. Bigfoot, Mothman, etc.), and then a List of Materials that would be applied to the SkinnedMeshRenderer to apply the skin.
[System.Serializable]
public class PlayerSkin
{
    public string skinName;
    public CryptidCharacterType cryptidSkinType;
    public List<Material> skinColors;
}
