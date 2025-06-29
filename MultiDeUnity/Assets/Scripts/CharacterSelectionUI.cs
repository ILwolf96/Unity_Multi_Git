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
        var runner = FindObjectOfType<Fusion.NetworkRunner>();
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].onClick.AddListener(() =>
            {
                CharacterSelectionManager.Instance.RPC_RequestCharacterSelection(runner.LocalPlayer, index);
            });
        }
    }

    public void HideUI()
    {
        gameObject.SetActive(false);
    }

    public void ShowDenialMessage()
    {
        Debug.LogWarning("This character is taken. Please choose another.");
        // Optionally show a UI message popup
    }
}
