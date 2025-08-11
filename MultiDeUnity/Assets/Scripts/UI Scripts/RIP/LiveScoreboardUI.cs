using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Fusion;

[DisallowMultipleComponent]
public class LiveScoreboardUI : MonoBehaviour
{
    [Header("UI Prefab & Container")]
    public GameObject entryPrefab;               // prefab with TextMeshProUGUI component
    public Canvas canvas;                        // the Canvas used for HUD (Screen Space recommended)
    public float refreshInterval = 0.25f;        // how often to refresh sorting

    [Header("Ranked Positions")]
    public Transform[] rankTransforms;           // index 0 -> top position (1st)

    [Header("Styling")]
    public Color localPlayerColor = new Color(0.9f, 0.9f, 0.2f);

    private NetworkRunner runner;
    private Dictionary<PlayerRef, GameObject> entryMap = new Dictionary<PlayerRef, GameObject>();
    private Camera mainCam;
    private Coroutine refreshCoroutine;

    private void Start()
    {
        mainCam = Camera.main;
        refreshCoroutine = StartCoroutine(RefreshLoop());
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            // If this MonoBehaviour or GameObject was destroyed, safely stop
            if (this == null || gameObject == null)
                yield break;

            UpdateRunnerRefIfNeeded();
            UpdateScoreboard();
            yield return new WaitForSeconds(refreshInterval);
        }
    }

    private void OnDestroy()
    {
        if (refreshCoroutine != null)
            StopCoroutine(refreshCoroutine);

        // Clean up created UI entries
        foreach (var kv in entryMap)
        {
            if (kv.Value != null)
                Destroy(kv.Value);
        }
        entryMap.Clear();
    }

    private void UpdateRunnerRefIfNeeded()
    {
        if (runner == null)
            runner = FindObjectOfType<NetworkRunner>();
        if (mainCam == null)
            mainCam = Camera.main;
    }

    private void UpdateScoreboard()
    {
        // Defensive checks: bail early if essential references missing or destroyed
        if (runner == null || entryPrefab == null || canvas == null || rankTransforms == null) return;
        if (canvas.transform == null) return; // canvas destroyed

        // Build player list
        var players = new List<(PlayerRef player, int score, bool isLocal, int charIdx)>();
        foreach (var p in runner.ActivePlayers)
        {
            var obj = runner.GetPlayerObject(p);
            if (obj == null) continue;
            var pc = obj.GetComponent<PlayerController>();
            int score = pc != null ? pc.GetScore() : 0;
            int cidx = pc != null ? pc.NetworkedCharacterIndex : -1;
            bool isLocal = p == runner.LocalPlayer;
            players.Add((p, score, isLocal, cidx));
        }

        var sorted = players.OrderByDescending(x => x.score).ThenBy(x => x.player.PlayerId).ToList();

        // Remove entries for players who left
        var keys = entryMap.Keys.ToList();
        foreach (var k in keys)
        {
            if (!sorted.Any(s => s.player == k))
            {
                if (entryMap[k] != null)
                    Destroy(entryMap[k]);
                entryMap.Remove(k);
            }
        }

        for (int rank = 0; rank < sorted.Count; rank++)
        {
            var item = sorted[rank];
            GameObject go;
            if (!entryMap.TryGetValue(item.player, out go) || go == null)
            {
                // instantiate new entry
                go = Instantiate(entryPrefab, canvas.transform, false);

                // Force RectTransform anchor/pivot/size so layout doesn't interfere
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    if (rt.sizeDelta == Vector2.zero)
                        rt.sizeDelta = new Vector2(220f, 60f);
                }

                // Remove layout components that may interfere with positioning
                var vg = go.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (vg != null) Destroy(vg);
                var hg = go.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                if (hg != null) Destroy(hg);
                var csf = go.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (csf != null) Destroy(csf);

                entryMap[item.player] = go;
            }

            // Update text
            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                string label = $"Player {item.player.PlayerId}";
                if (item.isLocal) label += " (YOU)";
                if (item.charIdx >= 0) label += $"  —  Char #{item.charIdx}";
                text.text = $"{label}\n{item.score}";
                text.color = item.isLocal ? localPlayerColor : Color.white;
            }

            // Position the entry according to rankTransforms (defensive: check for null)
            Transform targetTransform = null;
            if (rankTransforms != null && rankTransforms.Length > 0)
            {
                // find next non-null rank transform for this rank, fallback gracefully
                for (int t = rank; t < rankTransforms.Length; t++)
                {
                    if (rankTransforms[t] != null)
                    {
                        targetTransform = rankTransforms[t];
                        break;
                    }
                }
                // if we didn't find a matching index, use last non-null
                if (targetTransform == null)
                {
                    for (int t = rankTransforms.Length - 1; t >= 0; t--)
                        if (rankTransforms[t] != null) { targetTransform = rankTransforms[t]; break; }
                }
            }

            if (targetTransform != null && targetTransform != null)
            {
                Vector3 worldPos = targetTransform.position;
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCam, worldPos);
                RectTransform canvasRect = canvas.transform as RectTransform;
                if (canvasRect == null) continue;

                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCam, out localPoint);

                RectTransform entryRT = go.GetComponent<RectTransform>();
                if (entryRT != null)
                {
                    // Ensure entryRT is valid (not destroyed)
                    if (entryRT != null)
                    {
                        entryRT.anchoredPosition = localPoint;
                    }
                }
                else
                {
                    // fallback
                    go.transform.position = mainCam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));
                }
            }
        }
    }
}
