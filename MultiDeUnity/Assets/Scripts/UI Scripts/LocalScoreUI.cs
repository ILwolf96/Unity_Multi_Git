using UnityEngine;
using TMPro;
using Fusion;

[DisallowMultipleComponent]
public class LocalScoreUI : MonoBehaviour
{
    [Tooltip("TextMeshProUGUI that will display the local player's score")]
    public TextMeshProUGUI scoreText;

    [Tooltip(" label (\"Score:\")")]
    public string prefix = "Score: ";

    [Tooltip("Update interval in seconds")]
    public float updateInterval = 0.2f;

    private NetworkRunner runner;
    private float timer = 0f;

    private void Start()
    {
        runner = FindObjectOfType<NetworkRunner>();
        if (scoreText == null)
            Debug.LogWarning("[LocalScoreUI] scoreText not assigned, again? for real?.");
    }

    private void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = updateInterval;

        if (runner == null)
            runner = FindObjectOfType<NetworkRunner>();

        if (runner == null || !runner.IsRunning)
        {
            if (scoreText != null) scoreText.gameObject.SetActive(false);
            return;
        }


        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.MatchState.GameOver)
        {
            if (scoreText != null) scoreText.gameObject.SetActive(false);
            return;
        }

        // get local player's player object
        PlayerRef local = runner.LocalPlayer;
        NetworkObject pObj = runner.GetPlayerObject(local);
        if (pObj == null)
        {
            if (scoreText != null) scoreText.gameObject.SetActive(false);
            return;
        }

        var pc = pObj.GetComponent<PlayerController>();
        if (pc == null)
        {
            if (scoreText != null) scoreText.gameObject.SetActive(false);
            return;
        }

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            scoreText.text = $"{prefix}{pc.GetScore()}";
        }
    }
}
