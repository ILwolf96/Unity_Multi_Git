using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public struct PlayerInputData : INetworkInput
{
    public Vector2 move;
    public bool fire;
}

[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Networked] public int NetworkedCharacterIndex { get; set; }

    [Networked] public int Score { get; set; } = 0;

    [Networked] public bool IsAI { get; set; } = false;

    [Networked] public bool CanMove { get; set; } = false;

    // COMMENTED OUT: legacy networked transform fields (kept for easy reversion)
    // [Networked] private Vector3 NetworkedPosition { get; set; }
    // [Networked] private Quaternion NetworkedRotation { get; set; }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float maxRotationAngle = 45f;

    [SerializeField] InputActionReference MovementInputNew;

    [SerializeField] private Material[] playerMaterials;

    private float lastFireTime = 0f;

    public override void Spawned()
    {
        // legacy init removed (we are no longer using NetworkedPosition/Rotation)
        // NetworkedPosition = transform.position;
        // NetworkedRotation = transform.rotation;
        //lastFireTime = -fireCooldown;
        Runner.AddCallbacks(this);

        //ApplyPlayerMaterial(NetworkedCharacterIndex);
    }


    public override void FixedUpdateNetwork()
    {
        bool hasInput = GetInput<PlayerInputData>(out var input);

        if (Object.HasStateAuthority)
        {
            if (CanMove && hasInput)
            {
                Vector3 dir = new Vector3(input.move.x, 0f, input.move.y);
                if (dir.sqrMagnitude > 0.001f)
                {
                    // Host-authoritative: move the transform directly.
                    transform.position += dir.normalized * moveSpeed * Runner.DeltaTime;

                    Quaternion target = Quaternion.LookRotation(dir);
                    float maxA = Mathf.Min(rotationSpeed * Runner.DeltaTime, maxRotationAngle);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, target, maxA);
                }
            }
        }

        // IMPORTANT: do NOT overwrite transform on non-host peers.
        // NetworkTransform component on the prefab will replicate the host transform to clients.
    }


    public int GetScore() => Score;

    /*
    public void AddScore(int amount)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        Score += amount;
    }
    */

    public void AddScoreServer(int amount)
    {
        if (!Object.HasStateAuthority) return;
        Score += amount;
    }

    // Updated helpers: now operate on transform directly (host-authoritative).
    // These keep compatibility with code that calls SetNetworkedTransform / MoveNetworked.

    public void SetNetworkedTransform(Vector3 pos, Quaternion rot)
    {
        // If host/state-authority: set authoritative transform.
        if (Object.HasStateAuthority)
        {
            transform.position = pos;
            transform.rotation = rot;
            return;
        }

        // If called on client for immediate visual update, also set transform (safe).
        transform.position = pos;
        transform.rotation = rot;
    }

    public void MoveNetworked(Vector3 delta)
    {
        // Host-only: apply delta to transform. Clients should not invoke this.
        if (!Object.HasStateAuthority)
        {
            return;
        }
        transform.position += delta;
    }

    /*
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
    */




   #region INetworkRunnerCallbacks ()
    public void OnInput(NetworkRunner runner, NetworkInput inputPackage)
    {
        if (!Object.HasInputAuthority) return;

        inputPackage.Set(new PlayerInputData
        {
            move = MovementInputNew.action.ReadValue<Vector2>()
            //move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            // fire = Input.GetKey(KeyCode.Space)
        }) ;
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
