using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CryptidTypeHandler : NetworkBehaviour
{
    [Networked] public CryptidCharacterType CryptidCharacterType { get; set; }

    public override void Spawned()
    {
        Debug.Log($"[{Object.InputAuthority}] Spawned as a {CryptidCharacterType}");
    }
}
