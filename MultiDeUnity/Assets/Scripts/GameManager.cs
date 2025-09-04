using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static GameManager Instance { get; private set; }

    public enum MatchState : byte
    {
        CharacterSelection = 0,
        Running = 1,
        GameOver = 2
    }

    [Header("Match Settings")]
    [Tooltip("Seconds given to players to choose characters (host authoritative).")]
    public float selectionDuration = 30f;

    [Tooltip("Game duration in seconds.")]
    public float gameDuration = 150f;

    [Tooltip("How long to show final scores before returning to lobby.")]
    public float finalScreenDuration = 10f;

    [Header("AI Replacement")]
    [Tooltip("player prefab")]
    public NetworkObject playerPrefab;


    [Networked] public MatchState CurrentState { get; set; }
    [Networked] public float SelectionTimeLeft { get; set; }
    [Networked] public float GameTimeLeft { get; set; }
    [Networked] public float FinalTimeLeft { get; set; }

    private NetworkRunner runner;

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        runner = Runner;
        if (Runner != null)
        {
            Runner.AddCallbacks(this);
        }
        if (Object.HasStateAuthority)
        {
            CurrentState = MatchState.CharacterSelection;
            SelectionTimeLeft = selectionDuration;
            GameTimeLeft = gameDuration;
            FinalTimeLeft = finalScreenDuration;
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (CurrentState == MatchState.CharacterSelection)
        {
            SelectionTimeLeft -= Runner.DeltaTime;
            if (SelectionTimeLeft <= 0f)
            {
                SelectionTimeLeft = 0f;
                AutoAssignAndStart();
            }
            else
            {
                if (CharacterSelectionManager.Instance != null)
                {
                    int chosen = CharacterSelectionManager.Instance.GetSelectedCount();
                    int activePlayers = CountActivePlayers();
                    if (activePlayers > 0 && chosen >= activePlayers)
                    {
                        AutoAssignAndStart();
                    }
                }
            }
        }

        if (CurrentState == MatchState.Running)
        {
            GameTimeLeft -= Runner.DeltaTime;
            if (GameTimeLeft <= 0f)
            {
                GameTimeLeft = 0f;
                EndMatch();
            }
        }

        if (CurrentState == MatchState.GameOver)
        {
            FinalTimeLeft -= Runner.DeltaTime;
            if (FinalTimeLeft <= 0f)
            {
                FinalTimeLeft = 0f;
                RPC_ReturnToLobby();
            }
        }
    }

    private int CountActivePlayers()
    {
        int count = 0;
        foreach (var p in Runner.ActivePlayers)
            count++;
        return count;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestStartMatch()
    {
        if (!Object.HasStateAuthority)
            return;

        if (CurrentState != MatchState.CharacterSelection)
            return;

        if (CharacterSelectionManager.Instance != null)
        {
            foreach (var p in Runner.ActivePlayers)
            {
                if (!CharacterSelectionManager.Instance.HasPlayerSelected(p))
                {
                    int rnd = CharacterSelectionManager.Instance.GetRandomAvailableCharacterIndex();
                    if (rnd >= 0)
                        CharacterSelectionManager.Instance.Server_ForceSelect(p, rnd);
                }
            }
        }

        StartMatch();
    }

    public void StartMatch()
    {
        if (!Object.HasStateAuthority)
            return;

        CurrentState = MatchState.Running;

        GameTimeLeft = gameDuration;

        foreach (PlayerRef p in Runner.ActivePlayers)
        {
            var pObj = Runner.GetPlayerObject(p);
            if (pObj != null && pObj.TryGetComponent<PlayerController>(out var pc))
            {
                pc.CanMove = true;
            }
        }

        Debug.Log("[GameManager] Match started by host.");
    }

    public void AutoAssignAndStart()
    {
        if (!Object.HasStateAuthority) return;

        if (CharacterSelectionManager.Instance != null)
        {
            foreach (var p in Runner.ActivePlayers)
            {
                if (!CharacterSelectionManager.Instance.HasPlayerSelected(p))
                {
                    int rnd = CharacterSelectionManager.Instance.GetRandomAvailableCharacterIndex();
                    if (rnd >= 0)
                    {
                        CharacterSelectionManager.Instance.Server_ForceSelect(p, rnd);
                    }
                }
            }
        }

        StartMatch();
    }

    public void EndMatch()
    {
        Debug.Log("[GameManager] EndMatch called. CurrentState = " + CurrentState);

        if (!Object.HasStateAuthority) return;

        CurrentState = MatchState.GameOver;

        foreach (PlayerRef p in Runner.ActivePlayers)
        {
            var pObj = Runner.GetPlayerObject(p);
            if (pObj != null && pObj.TryGetComponent<PlayerController>(out var pc))
            {
                pc.CanMove = false;
            }
        }

        FinalTimeLeft = finalScreenDuration;

        Debug.Log("[GameManager] Match ended by timer. Showing final results.");
    }

    // Problem was racing, Host was leaving first, and all the clients got left behind
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ReturnToLobby() 
    {
        Debug.Log("[GameManager] RPC_ReturnToLobby received on peer, Hermes will take it from here.");
        Hermes.StartReturnToLobbySequence("SampleScene");
    }



    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log($"[GameManager] OnPlayerLeft called for Player {player.PlayerId}.");

        NetworkObject oldObj = runner.GetPlayerObject(player);

        if (oldObj == null)
        {
            Debug.LogWarning($"[GameManager] Player object for {player.PlayerId} not found. Spawning backup AI.");

            if (playerPrefab == null)
            {
                Debug.LogError("[GameManager] playerPrefab not assigned, cannot spawn backup AI.");
                return;
            }

            Vector3 fallbackPos = Vector3.zero;
            Quaternion fallbackRot = Quaternion.identity;

            NetworkObject aiObj = runner.Spawn(playerPrefab, fallbackPos, fallbackRot);
            if (aiObj == null)
            {
                Debug.LogError("[GameManager] Failed to spawn backup AI object.");
                return;
            }

            var aiPc = aiObj.GetComponent<PlayerController>();
            if (aiPc != null)
            {
                aiPc.IsAI = true;
                aiPc.CanMove = true;
            }

            var aiCtrl = aiObj.GetComponent<AIController>() ?? aiObj.gameObject.AddComponent<AIController>();
            aiCtrl.SetAsAI();

            Debug.Log($"[GameManager] Spawned AI for disconnected player {player.PlayerId}.");
            return;
        }

        var oldPc = oldObj.GetComponent<PlayerController>();
        int existingScore = 0;
        int charIdx = -1;
        Vector3 pos = oldObj.transform.position;
        Quaternion rot = oldObj.transform.rotation;

        if (oldPc != null)
        {
            existingScore = oldPc.GetScore();
            charIdx = oldPc.NetworkedCharacterIndex;
        }

        if (oldPc != null)
        {
            oldPc.IsAI = true;
            oldPc.CanMove = true;
            Debug.Log($"[GameManager] Marked object as AI for player {player.PlayerId} (char {charIdx}, score {existingScore}).");
        }

        try
        {
            oldObj.RemoveInputAuthority();
        }
        catch
        {
        }

        try
        {
            runner.SetPlayerObject(player, null);
        }
        catch { }

        var aiController = oldObj.GetComponent<AIController>();
        if (aiController == null)
        {
            aiController = oldObj.gameObject.AddComponent<AIController>();
        }

        try
        {
            aiController.SetAsAI();
        }
        catch
        {
            Debug.LogWarning("[GameManager] AIController.SetAsAI() call failed or is missing, wtf, how why where what?!.");
        }

        if (CharacterSelectionManager.Instance != null && charIdx >= 0)
        {
            try
            {
                CharacterSelectionManager.Instance.MarkCharacterTaken(charIdx);
            }
            catch { }
        }

        Debug.Log($"[GameManager] Converted disconnected player {player.PlayerId} -> AI on character {charIdx}.");
    }




    #region INetworkRunnerCallbacks ()

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    #endregion
}
