using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// Host-authoritative match manager.
// Place as a Networked scene object in the GameScene with a NetworkObject component.
// Controls Character Selection timer, match start, and match state.
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

    [Tooltip("Game duration in seconds (will be implemented later).")]
    public float gameDuration = 150f;

    [Networked] public MatchState CurrentState { get; set; }
    [Networked] public float SelectionTimeLeft { get; set; }
    [Networked] public float GameTimeLeft { get; set; }

    private NetworkRunner runner;

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        runner = Runner;
        // Only the StateAuthority (host) should initialize the timers.
        if (Object.HasStateAuthority)
        {
            CurrentState = MatchState.CharacterSelection;
            SelectionTimeLeft = selectionDuration;
            GameTimeLeft = gameDuration;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        // Selection countdown
        if (CurrentState == MatchState.CharacterSelection)
        {
            // Decrement using Runner.DeltaTime
            SelectionTimeLeft -= Runner.DeltaTime;
            if (SelectionTimeLeft <= 0f)
            {
                // when time's up, auto assign and start
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
                        StartMatch();
                    }
                }
            }
        }

        // Game timer (will count only when running)
        if (CurrentState == MatchState.Running)
        {
            GameTimeLeft -= Runner.DeltaTime;
            if (GameTimeLeft <= 0f)
            {
                GameTimeLeft = 0f;
                EndMatch();
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

    // RPC that allows any peer to request a match start; only the state authority (host) will act on it.
    // We keep this so UI can call it from any peer, but only host can effect the start.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestStartMatch()
    {
        if (!Object.HasStateAuthority)
            return;

        // only start if still in selection state
        if (CurrentState != MatchState.CharacterSelection)
            return;

        // to ensure min players, we can include a check here if we wish
        StartMatch();
    }

    // Called by the host (state authority) to start the match.
    public void StartMatch()
    {
        if (!Object.HasStateAuthority)
            return;

        CurrentState = MatchState.Running;

        // Make sure all players are allowed to move now
        foreach (PlayerRef p in Runner.ActivePlayers)
        {
            var pObj = Runner.GetPlayerObject(p);
            if (pObj != null && pObj.TryGetComponent<PlayerController>(out var pc))
            {
                pc.CanMove = true;
            }
        }

        // Optionally start coin spawners or other game systems (CoinSpawner polls game state)
        Debug.Log("[GameManager] Match started by host.");
    }

    // Auto-assign characters for players who haven't selected one, then start the match.
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

    // Call when the match ends.
    public void EndMatch()
    {
        if (!Object.HasStateAuthority) return;

        CurrentState = MatchState.GameOver;

        // You can trigger GameOver UI via RPC here, gather scores, etc.
        Debug.Log("[GameManager] Match ended by timer.");
    }
}
