using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Host-authoritative match manager.
/// Place as a Networked scene object in the GameScene with a NetworkObject component.
/// Controls Character Selection timer, match start, match timer, game over flow and return-to-lobby.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum MatchState : byte
    {
        CharacterSelection = 0,
        Running = 1,
        GameOver = 2
    }

    [Header("Match Settings")]
    [Tooltip("Seconds given to players to choose characters (server/host authoritative).")]
    public float selectionDuration = 30f;

    [Tooltip("Game duration in seconds.")]
    public float gameDuration = 150f;

    [Tooltip("How long to show final scores before returning to lobby.")]
    public float finalScreenDuration = 10f;

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

        // Selection countdown
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
                // If all players have already chosen, start early
                if (CharacterSelectionManager.Instance != null)
                {
                    int chosen = CharacterSelectionManager.Instance.GetSelectedCount();
                    int activePlayers = CountActivePlayers();
                    if (activePlayers > 0 && chosen >= activePlayers)
                    {
                        // Auto-start (host)
                        AutoAssignAndStart(); // assign none but call StartMatch path which starts immediately
                    }
                }
            }
        }

        // Game countdown
        if (CurrentState == MatchState.Running)
        {
            GameTimeLeft -= Runner.DeltaTime;
            if (GameTimeLeft <= 0f)
            {
                GameTimeLeft = 0f;
                EndMatch();
            }
        }

        // Final screen countdown (when in GameOver)
        if (CurrentState == MatchState.GameOver)
        {
            FinalTimeLeft -= Runner.DeltaTime;
            if (FinalTimeLeft <= 0f)
            {
                FinalTimeLeft = 0f;
                // Ask all peers to return to lobby
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

        // Start acts like timeout: auto-assign unpicked and start
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

        // Reset/initialize game timer when match starts
        GameTimeLeft = gameDuration;

        // Enable movement for all players
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

        // Stop player movement
        foreach (PlayerRef p in Runner.ActivePlayers)
        {
            var pObj = Runner.GetPlayerObject(p);
            if (pObj != null && pObj.TryGetComponent<PlayerController>(out var pc))
            {
                pc.CanMove = false;
            }
        }

        // Initialize final screen timer
        FinalTimeLeft = finalScreenDuration;

        // Optionally broadcast an RPC to show a game-over UI if needed elsewhere
        // The FinalScorePanel already watches GameManager.CurrentState to show itself.
        Debug.Log("[GameManager] Match ended by timer. Showing final results.");
    }

    // RPC that instructs everyone to return to the lobby (run from state authority)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ReturnToLobby()
    {
        // Called on all peers
        // Attempt to gracefully shutdown runner and load main menu
        if (Runner != null && Runner.IsRunning)
        {
            // local runner shutdown -- clients and host will call this
            try { Runner.Shutdown(); } catch { }
        }

        // Replace "SampleScene" with your main menu scene name if different
        SceneManager.LoadScene("SampleScene");
    }
}
