using UnityEngine;
using TMPro;

public class GameTimerUI : MonoBehaviour // Tick Tack
{
    public TextMeshProUGUI timerText;
    public bool showDuringSelection = false;

    private void Update()
    {
        if (GameManager.Instance == null)
        {
            if (timerText != null) timerText.gameObject.SetActive(false);
            return;
        }

        var gm = GameManager.Instance;
        if (gm.CurrentState == GameManager.MatchState.Running)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = FormatSeconds(gm.GameTimeLeft);
        }
        else if (gm.CurrentState == GameManager.MatchState.CharacterSelection && showDuringSelection)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = FormatSeconds(gm.SelectionTimeLeft);
        }
        else
        {
            timerText.gameObject.SetActive(false);
        }
    }

    private string FormatSeconds(float seconds)
    {
        seconds = Mathf.Max(0, seconds);
        int s = Mathf.CeilToInt(seconds);
        int mins = s / 60;
        int secs = s % 60;
        return $"{mins:00}:{secs:00}";
    }
}
