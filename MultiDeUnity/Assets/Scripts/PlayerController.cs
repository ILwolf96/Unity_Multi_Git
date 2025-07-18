using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public struct PlayerInputData : INetworkInput
{
    public Vector2 move;
    public bool fire;
}

[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Networked]
    public int NetworkedCharacterIndex { get; set; }

    [Networked] private Vector3 NetworkedPosition { get; set; }
    [Networked] private Quaternion NetworkedRotation { get; set; }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float maxRotationAngle = 45f;

    [Header("Shooting")]
    [SerializeField] private NetworkObject projectilePrefab = null;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float fireCooldown = 0.5f;

    [SerializeField] private Material[] playerMaterials;

    private float lastFireTime = 0f;

    public override void Spawned()
    {
        NetworkedPosition = transform.position;
        NetworkedRotation = transform.rotation;
        lastFireTime = -fireCooldown;
        Runner.AddCallbacks(this);

        ApplyPlayerMaterial(NetworkedCharacterIndex);
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput<PlayerInputData>(out var input))
            return;

        Vector3 dir = new Vector3(input.move.x, 0, input.move.y);
        if (dir.sqrMagnitude > 0.001f)
        {
            NetworkedPosition += dir * moveSpeed * Runner.DeltaTime;
            Quaternion target = Quaternion.LookRotation(dir);
            float maxA = Mathf.Min(rotationSpeed * Runner.DeltaTime, maxRotationAngle);
            NetworkedRotation = Quaternion.RotateTowards(NetworkedRotation, target, maxA);
        }

        if (input.fire && Runner.SimulationTime - lastFireTime >= fireCooldown)
        {
            if (Object.HasStateAuthority)
            {
                lastFireTime = Runner.SimulationTime;
                Shoot(NetworkedCharacterIndex);
            }
        }

        transform.position = NetworkedPosition;
        transform.rotation = NetworkedRotation;
    }

    private void Shoot(int characterIndex)
    {
        if (!Object.HasStateAuthority)
            return;

        Vector3 pos = transform.position + transform.forward * 1.5f;
        NetworkObject proj = Runner.Spawn(projectilePrefab, pos, transform.rotation, Object.InputAuthority);

        if (proj.TryGetComponent<Projectile>(out var script))
        {
            script.Initialize(transform.forward * projectileSpeed, characterIndex);
        }
    }

    private void ApplyPlayerMaterial(int charIndex)
    {
        if (playerMaterials == null || charIndex < 0 || charIndex >= playerMaterials.Length)
        {
            Debug.LogError($"Invalid character index {charIndex} or playerMaterials array not set in PlayerController on {name}.");
            return;
        }

        Renderer playerRend = GetComponentInChildren<Renderer>();
        if (playerRend != null)
        {
            playerRend.material = playerMaterials[charIndex];
        }
        else
        {
            Debug.LogWarning($"No Renderer found in children of PlayerController on {name} to apply material.");
        }
    }

    public override void Render()
    {
        ApplyPlayerMaterial(NetworkedCharacterIndex);
    }

    #region INetworkRunnerCallbacks
    public void OnInput(NetworkRunner runner, NetworkInput inputPackage)
    {
        if (!Object.HasInputAuthority) return;

        inputPackage.Set(new PlayerInputData
        {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            fire = Input.GetKey(KeyCode.Space)
        });
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}