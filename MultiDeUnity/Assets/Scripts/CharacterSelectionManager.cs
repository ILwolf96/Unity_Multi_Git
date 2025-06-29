using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion.Sockets;

public class CharacterSelectionManager : NetworkBehaviour
{
    public static CharacterSelectionManager Instance;

    [Header("Character Selection")]
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [Header("UI References")]
    [SerializeField] private GameObject characterSelectionPanel;
    [SerializeField] private Button[] characterButtons;
    [SerializeField] private TextMeshProUGUI denialMessageText;
    [SerializeField] private float denialMessageDuration = 1.0f;

    // Internal tracking
    private Dictionary<PlayerRef, int> playerSelections = new();
    private HashSet<int> takenCharacters = new();
    private HashSet<int> assignedSpawnPoints = new();

    private NetworkRunner runner;

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Debug.Log("CharacterSelectionManager spawned as MasterClient.");
        }

        runner = Runner;
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (characterButtons == null || characterButtons.Length == 0)
        {
            Debug.LogWarning("Character buttons not assigned.");
            return;
        }

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].onClick.RemoveAllListeners();
            characterButtons[i].onClick.AddListener(() =>
            {
                Debug.Log($"Player {Runner.LocalPlayer.PlayerId} clicked character {index}");
                RPC_RequestCharacterSelection(Runner.LocalPlayer, index);
            });
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestCharacterSelection(PlayerRef requester, int characterIndex)
    {
        if (!Object.HasStateAuthority) return;

        if (takenCharacters.Contains(characterIndex))
        {
            Debug.Log($"Character {characterIndex} already taken. Denied for {requester.PlayerId}.");
            RPC_SelectionDenied(requester);
            return;
        }

        int spawnIndex = GetAvailableSpawnPointIndex();
        if (spawnIndex == -1)
        {
            Debug.LogWarning("No spawn points available!");
            RPC_SelectionDenied(requester);
            return;
        }

        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        Quaternion spawnRot = spawnPoints[spawnIndex].rotation;

        GameObject prefab = characterPrefabs[characterIndex];
        if (prefab == null || prefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError($"Character prefab at index {characterIndex} is invalid or missing NetworkObject.");
            RPC_SelectionDenied(requester);
            return;
        }

        Runner.Spawn(prefab, spawnPos, spawnRot, requester);

        takenCharacters.Add(characterIndex);
        playerSelections[requester] = characterIndex;
        assignedSpawnPoints.Add(spawnIndex);

        Debug.Log($"Character {characterIndex} approved for {requester.PlayerId} at spawn {spawnIndex}");
        RPC_SelectionApproved(requester);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SelectionApproved(PlayerRef player)
    {
        if (Runner.LocalPlayer == player)
        {
            Debug.Log("Character selection approved.");
            if (characterSelectionPanel != null)
                characterSelectionPanel.SetActive(false);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SelectionDenied(PlayerRef player)
    {
        if (Runner.LocalPlayer == player)
        {
            Debug.LogWarning("Character selection denied. Please choose another.");
            if (denialMessageText != null)
                StartCoroutine(ShowDeniedMessage("Character taken. Please choose another."));
        }
    }

    private IEnumerator ShowDeniedMessage(string message)
    {
        denialMessageText.text = message;
        denialMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(denialMessageDuration);
        denialMessageText.gameObject.SetActive(false);
    }

    private int GetAvailableSpawnPointIndex()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!assignedSpawnPoints.Contains(i))
                return i;
        }
        return -1;
    }

    public void ReleaseCharacter(PlayerRef player)
    {
        if (playerSelections.TryGetValue(player, out int index))
        {
            takenCharacters.Remove(index);
            playerSelections.Remove(player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        ReleaseCharacter(player);
    }

    #region Unused INetworkRunnerCallbacks
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress addr, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}
