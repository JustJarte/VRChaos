using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "VRChaos/Spawn Locations")]
public class SpawnLocationsSO : ScriptableObject
{
    public List<SpawnLocationInfo> spawnLocations = new List<SpawnLocationInfo>();
}
