using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;
using System.Linq;

public class ScoreboardUI : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Parent transform (Content) where entry prefabs will be instantiated")]
    public Transform entriesContainer;

    [Tooltip("Simple prefab containing a TextMeshProUGUI component (use for each player)")]
    public GameObject entryPrefab;

    [Tooltip("How often (seconds) to refresh the scoreboard display")]
    public float refreshInterval = 0.25f;

    [Header("Styling")]
    [Tooltip("Text color for the local player entry")]
    public Color localPlayerColor = new Color(0.9f, 0.9f, 0.2f);

    private NetworkRunner runner;
    private Dictionary<PlayerRef, GameObject> entryMap = new Dictionary<PlayerRef, GameObject>();

    private void Start()
    {
        StartCoroutine(RefreshLoop());
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            UpdateRunnerRefIfNeeded();
            UpdateScoreboard();
            yield return new WaitForSeconds(refreshInterval);
        }
    }

    private void UpdateRunnerRefIfNeeded()
    {
        if (runner == null)
            runner = FindObjectOfType<NetworkRunner>();
    }

    private void UpdateScoreboard()
    {
        if (runner == null || entriesContainer == null || entryPrefab == null)
            return;

        // Build list of (PlayerRef, score, isLocal, charIdx)
        var list = new List<(PlayerRef player, int score, bool isLocal, int charIdx)>();

        foreach (var p in runner.ActivePlayers)
        {
            NetworkObject pObj = runner.GetPlayerObject(p);
            if (pObj == null)
                continue;

            var pc = pObj.GetComponent<PlayerController>();
            int score = 0;
            int charIdx = -1;
            if (pc != null)
            {
                score = pc.GetScore();         // uses PlayerController.GetScore() if available
                charIdx = pc.NetworkedCharacterIndex;
            }

            bool isLocal = p == runner.LocalPlayer;

            list.Add((p, score, isLocal, charIdx));
        }

        // sort descending by score
        var sorted = list.OrderByDescending(x => x.score).ThenBy(x => x.player.PlayerId).ToList();

        // Remove UI entries that correspond to players no longer present
        var keys = entryMap.Keys.ToList();
        foreach (var k in keys)
        {
            if (!sorted.Any(s => s.player == k))
            {
                Destroy(entryMap[k]);
                entryMap.Remove(k);
            }
        }

        // Update existing entries and create new ones
        foreach (var item in sorted)
        {
            GameObject go;
            if (!entryMap.TryGetValue(item.player, out go))
            {
                go = Instantiate(entryPrefab, entriesContainer, false);
                entryMap[item.player] = go;
            }

            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text == null) continue;

            string label = $"Player {item.player.PlayerId}";
            if (item.isLocal) label += " (YOU)";
            if (item.charIdx >= 0) label += $"  —  Char #{item.charIdx}";

            text.text = $"{label}\t{item.score}";

            // Style local player
            if (item.isLocal)
            {
                text.color = localPlayerColor;
                text.fontStyle = FontStyles.Bold;
            }
            else
            {
                text.color = Color.white;
                text.fontStyle = FontStyles.Normal;
            }
        }
    }
}
