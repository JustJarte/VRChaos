using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PrefabSource : MonoBehaviour, INetworkPrefabSource
{
    [SerializeField] private NetworkPrefabRef playerPrefab;

    NetworkObjectGuid INetworkPrefabSource.AssetGuid => throw new System.NotImplementedException();

    bool INetworkAssetSource<NetworkObject>.IsCompleted => throw new System.NotImplementedException();

    string INetworkAssetSource<NetworkObject>.Description => throw new System.NotImplementedException();

    public IEnumerable<NetworkPrefabRef> GetPrefabs()
    {
        yield return playerPrefab;
    }

    void INetworkAssetSource<NetworkObject>.Acquire(bool synchronous)
    {
        throw new System.NotImplementedException();
    }

    void INetworkAssetSource<NetworkObject>.Release()
    {
        throw new System.NotImplementedException();
    }

    NetworkObject INetworkAssetSource<NetworkObject>.WaitForResult()
    {
        throw new System.NotImplementedException();
    }
}
