using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    public static List<Coin> ActiveCoins = new List<Coin>();

    [Header("Coin Settings")]
    [SerializeField] private int coinValue = 5;
    [SerializeField] private float lifetime = 30f;

    private float spawnTime = 0f;

    public override void Spawned()
    {
        base.Spawned();

        spawnTime = Time.time;

        if (!ActiveCoins.Contains(this))
            ActiveCoins.Add(this);

        // Debug.Log($"[Coin] Spawned. HasStateAuthority={Object?.HasStateAuthority}, instance={gameObject.name}");
    }

    // Called when Runner.Despawn is invoked (It is better to use that over OnDestroy in Fusion I been told)
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        if (ActiveCoins.Contains(this))
            ActiveCoins.Remove(this);
    }

    private void OnDestroy()
    {
        if (ActiveCoins.Contains(this))
            ActiveCoins.Remove(this);
    }

    private void Update()
    {
        if (Object == null) return;

        if (Object.HasStateAuthority)
        {
            if (Time.time - spawnTime >= lifetime)
            {
                CoinSpawner.Instance?.NotifyCoinDespawned();
                try
                {
                    Runner.Despawn(Object);
                }
                catch { }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetSpawnTransform(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }

    private void OnTriggerEnter(Collider other)
    {
        var no = other.GetComponentInParent<NetworkObject>();
        if (no == null) return;

        PlayerRef requestingPlayer = no.InputAuthority;
        if (requestingPlayer == PlayerRef.None)
            return;

        RPC_RequestPickup(requestingPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestPickup(PlayerRef requestingPlayer)
    {
        if (!Object.HasStateAuthority)
            return;

        NetworkObject playerObj = Runner.GetPlayerObject(requestingPlayer);
        if (playerObj == null) return;

        if (playerObj.TryGetComponent<PlayerController>(out var pc))
        {
            pc.AddScoreServer(coinValue);
        }

        CoinSpawner.Instance?.NotifyCoinDespawned();
        Runner.Despawn(Object);
    }

    public void ProcessPickupBy(PlayerController picker)
    {
        if (!Object.HasStateAuthority) return; 

        if (picker != null)
        {
            picker.AddScoreServer(coinValue);
        }

        CoinSpawner.Instance?.NotifyCoinDespawned();
        Runner.Despawn(Object);
    }
}
