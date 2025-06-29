using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class CharacterSelectionManager : NetworkBehaviour
{
   
    public static CharacterSelectionManager Instance;

    // Track characters
    private Dictionary<PlayerRef, int> playerSelections = new Dictionary<PlayerRef, int>();
    private HashSet<int> takenCharacters = new HashSet<int>();

    // Prefabs to spawn
    [SerializeField] private GameObject[] characterPrefabs;

    // Spawn points
    [SerializeField] private Transform[] spawnPoints;

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
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestCharacterSelection(PlayerRef requester, int characterIndex)
    {
        if (!Object.HasStateAuthority)
            return;

        if (takenCharacters.Contains(characterIndex))
        {
            Debug.Log($"Character {characterIndex} already taken. Denied for {requester.PlayerId}.");
            RPC_SelectionDenied(requester);
            return;
        }

        Debug.Log($"Character {characterIndex} selected by {requester.PlayerId}. Approved.");

        takenCharacters.Add(characterIndex);
        playerSelections[requester] = characterIndex;

        int spawnIndex = Mathf.Clamp(characterIndex, 0, spawnPoints.Length - 1);
        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        Quaternion spawnRot = spawnPoints[spawnIndex].rotation;

        Runner.Spawn(characterPrefabs[characterIndex], spawnPos, spawnRot, requester);

        RPC_SelectionApproved(requester);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SelectionApproved(PlayerRef player)
    {
        if (Runner.LocalPlayer == player)
        {
            Debug.Log("Character selection approved.");
            // TO DO: disable character selection UI here.
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SelectionDenied(PlayerRef player)
    {
        if (Runner.LocalPlayer == player)
        {
            Debug.LogWarning("Character selection denied. Please choose another.");
            // TO DO: display denial UI here.
        }
    }

    public void ReleaseCharacter(PlayerRef player)
    {
        if (playerSelections.TryGetValue(player, out int index))
        {
            takenCharacters.Remove(index);
            playerSelections.Remove(player);
        }
    }

}
