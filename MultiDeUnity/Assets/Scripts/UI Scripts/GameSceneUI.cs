using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class GameSceneUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject startButtonObject = null; // contains Button for host, IT IS A HOST ONLY Object!
    [SerializeField] private Button startButton = null;
    [SerializeField] private GameObject waitingForHostObject = null; // show when not host, aka Thy Client
    [SerializeField] private TMP_Text selectionTimerText = null;

    private NetworkRunner runner;

    private void Start()
    {
        runner = FindObjectOfType<NetworkRunner>();

        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonPressed);

        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
    }

private void UpdateUI()
{
    if (GameManager.Instance == null || runner == null)
    {
        if (startButtonObject != null) startButtonObject.SetActive(false);
        if (waitingForHostObject != null) waitingForHostObject.SetActive(false);
        if (selectionTimerText != null) selectionTimerText.gameObject.SetActive(false);
        return;
    }

    if (GameManager.Instance.CurrentState != GameManager.MatchState.CharacterSelection)
    {
        if (startButtonObject != null) startButtonObject.SetActive(false);
        if (waitingForHostObject != null) waitingForHostObject.SetActive(false);
        if (selectionTimerText != null) selectionTimerText.gameObject.SetActive(false);
        return;
    }

    bool amHost = GameManager.Instance.Object.HasStateAuthority;

    if (startButtonObject != null)
        startButtonObject.SetActive(amHost);

    if (waitingForHostObject != null)
        waitingForHostObject.SetActive(!amHost);

    if (selectionTimerText != null)
    {
        selectionTimerText.gameObject.SetActive(true);
        float left = GameManager.Instance.SelectionTimeLeft;
        selectionTimerText.text = $"Select character: {Mathf.CeilToInt(left)}s";
    }
}


    private void OnStartButtonPressed()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.RPC_RequestStartMatch();
    }
}
