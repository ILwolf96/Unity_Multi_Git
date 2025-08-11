using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    [SerializeField] private int coinValue = 5; // 5 or 10 depending on prefab
    [SerializeField] private float lifetime = 30f;

    private float spawnTime = 0f;

    public override void Spawned()
    {
        spawnTime = Time.time;
        //Debug.Log($"[Coin] Spawned on peer (HasStateAuthority={Object.HasStateAuthority}) name={gameObject.name} instanceID={gameObject.GetInstanceID()}");

    }

    private void Update()
    {
        if (Object == null) return;

        // Only the state authority (host) will despawn the coins when lifetime expires
        if (Object.HasStateAuthority)
        {
            if (Time.time - spawnTime >= lifetime)
            {
                try
                {
                    // Notify spawner (if any) before despawn
                    CoinSpawner.Instance?.NotifyCoinDespawned();

                    Runner.Despawn(Object);
                }
                catch { }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetSpawnTransform(Vector3 pos, Quaternion rot)
    {
        // Always set the transform to the authoritative spawn transform
        transform.position = pos;
        transform.rotation = rot;
    }


    private void OnTriggerEnter(Collider other)
    {
        // Only attempt pickup if the other GameObject has a NetworkObject
        var no = other.GetComponentInParent<NetworkObject>();
        if (no == null) return;

        // Determine which player owns the colliding object (input authority).
        PlayerRef requestingPlayer = no.InputAuthority;
        if (requestingPlayer == PlayerRef.None)
            return;

        // Request pickup on the coin's state authority (host).
        RPC_RequestPickup(requestingPlayer);
    }

    // RPC to request pickup to the coin's state authority (host).
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestPickup(PlayerRef requestingPlayer)
    {
        if (!Object.HasStateAuthority)
            return;
        //Debug.Log($"[Coin.RPC_RequestPickup] Received for Player {requestingPlayer.PlayerId} on peer (HasStateAuthority={Object.HasStateAuthority}) coinName={gameObject.name} instanceID={gameObject.GetInstanceID()}");        // Verify the player still exists and has a player object
        NetworkObject playerObj = Runner.GetPlayerObject(requestingPlayer);
        if (playerObj == null) return;

        if (playerObj.TryGetComponent<PlayerController>(out var pc))
        {
            pc.AddScore(coinValue);
        }

        // Notify spawner (if present) before despawn so it can decrement its counter
        CoinSpawner.Instance?.NotifyCoinDespawned();

        // Despawn the coin (host/state authority)
        Runner.Despawn(Object);
    }
}
