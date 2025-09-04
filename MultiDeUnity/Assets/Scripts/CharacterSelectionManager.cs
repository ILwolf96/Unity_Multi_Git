using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private Dictionary<PlayerRef, int> playerSelections = new();
    private HashSet<int> takenCharacters = new();
    private HashSet<int> assignedSpawnPoints = new();
    private Dictionary<PlayerRef, int> playerSpawnIndex = new();

    private NetworkRunner runner;

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        runner = Runner;
        SetupButtons();
    }

    private void SetupButtons()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].onClick.RemoveAllListeners();
            characterButtons[i].onClick.AddListener(() =>
            {
                RPC_RequestCharacterSelection(Runner.LocalPlayer, index);
            });
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestCharacterSelection(PlayerRef requester, int characterIndex)
    {
        if (!Object.HasStateAuthority)
            return;

        if (takenCharacters.Contains(characterIndex))
        {
            RPC_SelectionDenied(requester);
            return;
        }

        int spawnIndex = GetAvailableSpawnPointIndex();
        if (spawnIndex < 0)
        {
            RPC_SelectionDenied(requester);
            return;
        }

        GameObject prefab = characterPrefabs[characterIndex];
        if (prefab == null || prefab.GetComponent<NetworkObject>() == null)
        {
            RPC_SelectionDenied(requester);
            return;
        }

        Vector3 pos = spawnPoints[spawnIndex].position;
        Quaternion rot = spawnPoints[spawnIndex].rotation;

        NetworkObject networkPlayerObject = runner.Spawn(prefab, pos, rot, inputAuthority: requester);

        runner.SetPlayerObject(requester, networkPlayerObject);

        takenCharacters.Add(characterIndex);
        playerSelections[requester] = characterIndex;
        assignedSpawnPoints.Add(spawnIndex);
        playerSpawnIndex[requester] = spawnIndex;

        if (networkPlayerObject.TryGetComponent<PlayerController>(out var pc))
        {
            
            pc.NetworkedCharacterIndex = characterIndex;
        }
        else
        {
            Debug.LogWarning($"Spawned player object does not contain PlayerController component (index {characterIndex}).");
        }

        RPC_SelectionApproved(requester);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SelectionApproved(PlayerRef player)
    {
        if (Runner.LocalPlayer == player && characterSelectionPanel != null)
            characterSelectionPanel.SetActive(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SelectionDenied(PlayerRef player)
    {
        if (Runner.LocalPlayer == player && denialMessageText != null)
            StartCoroutine(ShowDeniedMessage("Character already taken. Please choose another."));
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
            if (!assignedSpawnPoints.Contains(i))
                return i;
        return -1;
    }

    public void ReleaseCharacter(PlayerRef player)
    {
        if (playerSelections.TryGetValue(player, out int idx))
        {
            takenCharacters.Remove(idx);
            playerSelections.Remove(player);
        }

        if (playerSpawnIndex.TryGetValue(player, out int spawnIdx))
        {
            assignedSpawnPoints.Remove(spawnIdx);
            playerSpawnIndex.Remove(player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        NetworkObject playerObj = runner.GetPlayerObject(player);
        if (playerObj != null)
        {
            try
            {
                runner.Despawn(playerObj);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to despawn player object for Player {player.PlayerId}: {ex.Message}");
            }

            runner.SetPlayerObject(player, null);
        }

        ReleaseCharacter(player);
    }

    //----------- 

    public int GetSelectedCount()
    {
        return playerSelections.Count;
    }

    public bool HasPlayerSelected(PlayerRef player)
    {
        return playerSelections.ContainsKey(player);
    }

    public void MarkCharacterTaken(int characterIndex)
    {
        if (!Object.HasStateAuthority)
            return;

        if (!takenCharacters.Contains(characterIndex))
            takenCharacters.Add(characterIndex);
    }


    public int GetRandomAvailableCharacterIndex()
    {
        List<int> available = new List<int>();
        for (int i = 0; i < characterPrefabs.Length; i++)
            if (!takenCharacters.Contains(i))
                available.Add(i);

        if (available.Count == 0) return -1;
        int idx = Random.Range(0, available.Count);
        return available[idx];
    }

    public void Server_ForceSelect(PlayerRef requester, int characterIndex)
    {
        if (!Object.HasStateAuthority)
            return;

        if (takenCharacters.Contains(characterIndex))
            return;

        int spawnIndex = GetAvailableSpawnPointIndex();
        if (spawnIndex < 0)
            return;

        GameObject prefab = characterPrefabs[characterIndex];
        if (prefab == null || prefab.GetComponent<NetworkObject>() == null)
            return;

        Vector3 pos = spawnPoints[spawnIndex].position;
        Quaternion rot = spawnPoints[spawnIndex].rotation;

        NetworkObject networkPlayerObject = runner.Spawn(prefab, pos, rot, inputAuthority: requester);

        runner.SetPlayerObject(requester, networkPlayerObject);

        takenCharacters.Add(characterIndex);
        playerSelections[requester] = characterIndex;
        assignedSpawnPoints.Add(spawnIndex);
        playerSpawnIndex[requester] = spawnIndex;

        if (networkPlayerObject.TryGetComponent<PlayerController>(out var pc))
        {
            pc.NetworkedCharacterIndex = characterIndex;
            pc.CanMove = false;
        }

        RPC_SelectionApproved(requester);
    }

    #region INetworkRunnerCallbacks

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress addr, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
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
