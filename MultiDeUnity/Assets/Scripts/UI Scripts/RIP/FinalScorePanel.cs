using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Fusion;

public class FinalScorePanel : MonoBehaviour
{
    [Header("Panel UI")]
    public GameObject panelRoot;             
    public Transform entriesContainer;       
    public GameObject entryPrefab;           
    public TextMeshProUGUI finalTimerText;   

    private NetworkRunner runner;
    private bool shown = false;

    private void Start()
    {
        runner = FindObjectOfType<NetworkRunner>();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (runner == null) runner = FindObjectOfType<NetworkRunner>();
        if (GameManager.Instance == null) return;

        // show once when game over
        if (!shown && GameManager.Instance.CurrentState == GameManager.MatchState.GameOver)
        {
            PopulateAndShow();
            shown = true;
        }

        // update final timer label if present
        if (shown && GameManager.Instance.CurrentState == GameManager.MatchState.GameOver && finalTimerText != null)
        {
            finalTimerText.text = $"Returning in {Mathf.CeilToInt(GameManager.Instance.FinalTimeLeft)}s";
        }
    }

    private void PopulateAndShow()
    {
        Debug.Log("[FinalScorePanel] PopulateAndShow called");

        // basic defensive checks
        if (panelRoot == null)
        {
            Debug.LogError("[FinalScorePanel] panelRoot is not assigned in inspector. Cannot show final panel.");
            return;
        }

        if (entriesContainer == null || entryPrefab == null)
        {
            Debug.LogWarning("[FinalScorePanel] entriesContainer or entryPrefab not set. Panel will still be activated but no entries created.");
        }

        // Activate panel and move it to front of canvas
        panelRoot.SetActive(true);
        var parentCanvas = panelRoot.GetComponentInParent<Canvas>();
        if (parentCanvas != null) parentCanvas.sortingOrder = 1000;
        panelRoot.transform.SetAsLastSibling();

        // Clear existing entries safely
        if (entriesContainer != null)
        {
            var children = new List<Transform>();
            foreach (Transform t in entriesContainer) children.Add(t);
            foreach (Transform t in children)
                if (t != null) Destroy(t.gameObject);
        }

        if (runner == null) runner = FindObjectOfType<NetworkRunner>();

        // build sorted list
        var list = new List<(PlayerRef p, int score, int charIdx)>();
        if (runner != null)
        {
            foreach (var p in runner.ActivePlayers)
            {
                var obj = runner.GetPlayerObject(p);
                if (obj == null) continue;
                var pc = obj.GetComponent<PlayerController>();
                int sc = pc != null ? pc.GetScore() : 0;
                int cidx = pc != null ? pc.NetworkedCharacterIndex : -1;
                list.Add((p, sc, cidx));
            }
        }

        var sorted = list.OrderByDescending(x => x.score).ThenBy(x => x.p.PlayerId).ToList();

        // instantiate rows
        if (entriesContainer != null && entryPrefab != null)
        {
            foreach (var item in sorted)
            {
                var go = Instantiate(entryPrefab, entriesContainer, false);
                var txt = go.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    string label = $"Player {item.p.PlayerId}";
                    if (item.p == runner.LocalPlayer) label += " (YOU)";
                    if (item.charIdx >= 0) label += $" - Char #{item.charIdx}";
                    txt.text = $"{label}\t{item.score}";
                }
            }
        }
        else
        {
            Debug.Log("[FinalScorePanel] No entries created (missing container or prefab), but panel activated.");
        }
    }
}
