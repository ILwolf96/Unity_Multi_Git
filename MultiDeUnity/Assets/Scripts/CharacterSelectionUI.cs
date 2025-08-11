using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    public static CharacterSelectionUI Instance;

    [SerializeField] private Button[] characterButtons;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // If CharacterSelectionManager already exists it will perform button wiring itself.
        // To avoid duplicate listeners we simply disable this component in that case.
        if (CharacterSelectionManager.Instance != null)
        {
            // Optional: keep component but do not add listeners.
            enabled = false;
            return;
        }

        var runner = FindObjectOfType<Fusion.NetworkRunner>();
        if (runner == null)
        {
            Debug.LogWarning("CharacterSelectionUI: No NetworkRunner found. Buttons will not be wired.");
            return;
        }

        // Add listeners — if the manager isn't available yet they will call the manager when it becomes available.
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            // Remove any pre-existing listeners to be safe
            characterButtons[i].onClick.RemoveAllListeners();
            characterButtons[i].onClick.AddListener(() =>
            {
                // Guard in case manager hasn't been created yet. We'll attempt to call the RPC on the manager instance.
                if (CharacterSelectionManager.Instance != null)
                {
                    CharacterSelectionManager.Instance.RPC_RequestCharacterSelection(runner.LocalPlayer, index);
                }
                else
                {
                    Debug.LogWarning("CharacterSelectionUI: CharacterSelectionManager not present yet. Selection request skipped.");
                }
            });
        }
    }

    private void OnDestroy()
    {
        // Clean up listeners to avoid leaking duplicate listeners across domain reloads or scene changes.
        if (characterButtons != null)
        {
            foreach (var btn in characterButtons)
            {
                if (btn != null)
                    btn.onClick.RemoveAllListeners();
            }
        }
    }

    public void HideUI()
    {
        gameObject.SetActive(false);
    }

    public void ShowDenialMessage()
    {
        Debug.LogWarning("This character is taken. Please choose another.");
        // I can add to show a UI message popup
    }
}
