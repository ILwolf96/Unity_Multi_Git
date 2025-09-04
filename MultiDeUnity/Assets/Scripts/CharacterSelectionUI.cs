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
        if (CharacterSelectionManager.Instance != null)
        {
            enabled = false;
            return;
        }

        var runner = FindObjectOfType<Fusion.NetworkRunner>();
        if (runner == null)
        {
            Debug.LogWarning("CharacterSelectionUI: No NetworkRunner found, Buttons will not be wired, all is doomed, and it's all your fault.");
            return;
        }

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            // Remove any pre-existing listeners, just to be safe
            characterButtons[i].onClick.RemoveAllListeners();
            // Add listeners 
            characterButtons[i].onClick.AddListener(() =>
            {
                // incase manager hasn't been created yet.
                if (CharacterSelectionManager.Instance != null)
                {
                    CharacterSelectionManager.Instance.RPC_RequestCharacterSelection(runner.LocalPlayer, index); // call the RPC on the manager instance.
                }
                else
                {
                    Debug.LogWarning("CharacterSelectionUI: CharacterSelectionManager not present yet. Selection request shall be skipped, but please fix it asap.");
                }
            });
        }
    }

    private void OnDestroy()
    {
        // Clean up listeners to avoid leaking duplicate listeners across reloads or scene changes. (note, didn't quite fixed the errors on scene Change)
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
        // I can add to show a UI message popup, if I have time, and will
    }
}
