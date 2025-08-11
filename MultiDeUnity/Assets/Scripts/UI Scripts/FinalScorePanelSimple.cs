using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Fusion;


public class FinalScorePanelSimple : MonoBehaviour
{
    [Header("Panel & entries")]
    [Tooltip("The root GameObject for the final scoreboard panel (initially inactive).")]
    public GameObject panelRoot;

    [Tooltip("TMP text fields for each slot (index 0 = top rank).")]
    public TextMeshProUGUI[] entryTexts;

    [Tooltip("Optional TMP to show final countdown (e.g., 'Returning in 10s').")]
    public TextMeshProUGUI finalTimerText;

    [Header("Update")]
    [Tooltip("How often (seconds) to refresh the live values while playing.")]
    public float refreshInterval = 0.25f;

    private NetworkRunner runner;
    private bool shown = false;
    private float timer = 0f;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false); // ensure initial state
    }

    private void Start()
    {
        runner = FindObjectOfType<NetworkRunner>();
        // Defensive: if entries not set, log once
        if (entryTexts == null || entryTexts.Length == 0)
            Debug.LogWarning("[FinalScorePanelSimple] No entryTexts assigned. Please assign TMP slots in inspector.");
    }

    private void Update()
    {
        if (runner == null) runner = FindObjectOfType<NetworkRunner>();
        if (runner == null) return;

        if (GameManager.Instance == null) return;

        // Refresh live values at interval
        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f)
        {
            timer = refreshInterval;
            RefreshLiveValues();
        }

        // Show final panel when GameOver (only once)
        if (!shown && GameManager.Instance.CurrentState == GameManager.MatchState.GameOver)
        {
            ShowFinalPanel();
            shown = true;
            panelRoot.SetActive(true);
        }

        // Update final timer label if present and panel shown
        if (shown && finalTimerText != null)
        {
            finalTimerText.text = $"Returning in {Mathf.CeilToInt(GameManager.Instance.FinalTimeLeft)}s";
        }
    }

    private void RefreshLiveValues()
    {
        if (entryTexts == null || entryTexts.Length == 0) return;
        if (runner == null) return;

        // Gather players and their scores
        var list = new List<(PlayerRef p, int score, int charIdx)>();
        foreach (var p in runner.ActivePlayers)
        {
            var obj = runner.GetPlayerObject(p);
            if (obj == null) continue;
            var pc = obj.GetComponent<PlayerController>();
            int sc = pc != null ? pc.GetScore() : 0;
            int cidx = pc != null ? pc.NetworkedCharacterIndex : -1;
            list.Add((p, sc, cidx));
        }

        // sort descending by score
        var sorted = list.OrderByDescending(x => x.score).ThenBy(x => x.p.PlayerId).ToList();

        // Fill the entry slots (index 0 => best). If fewer players than slots, remaining slots get hidden/cleared.
        for (int i = 0; i < entryTexts.Length; i++)
        {
            var txt = entryTexts[i];
            if (txt == null) continue;

            if (i < sorted.Count)
            {
                var item = sorted[i];
                string label = $"Player {item.p.PlayerId}";
                if (item.p == runner.LocalPlayer) label += " (YOU)";
                if (item.charIdx >= 0) label += $" - Char {item.charIdx}";
                txt.gameObject.SetActive(true);
                txt.text = $"{i + 1}. {label}\t{item.score}";
            }
            else
            {
                // hide empty slots
                txt.gameObject.SetActive(false);
            }
        }
    }

    private void ShowFinalPanel()
    {
        if (panelRoot == null)
        {
            Debug.LogError("[FinalScorePanelSimple] panelRoot not assigned.");
            return;
        }

        // Ensure it's active and on top of Canvas
        panelRoot.SetActive(true);
        var c = panelRoot.GetComponentInParent<Canvas>();
        if (c != null) c.sortingOrder = 1000;
        panelRoot.transform.SetAsLastSibling();

        // Force final update immediately
        RefreshLiveValues();

        // Also update finalTimerText once
        if (finalTimerText != null && GameManager.Instance != null)
            finalTimerText.text = $"Returning in {Mathf.CeilToInt(GameManager.Instance.FinalTimeLeft)}s";
    }
}
