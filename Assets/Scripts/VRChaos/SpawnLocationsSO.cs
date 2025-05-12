using System.Collections.Generic;
using UnityEngine;

// ScriptableObject that holds a collection of all created spawn locations. This List is accessed from NetworkRunnerHandler to tell the Runner where to Spawn the player at based on Game Mode.
[CreateAssetMenu(menuName = "VRChaos/Spawn Locations")]
public class SpawnLocationsSO : ScriptableObject
{
    public List<SpawnLocationInfo> spawnLocations = new List<SpawnLocationInfo>();
}
