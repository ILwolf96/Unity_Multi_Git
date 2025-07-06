using UnityEngine;
using TMPro;
using Fusion;
using System.Collections.Generic;

public class PrivateChatManager : NetworkBehaviour
{
    [Header("Chat UI References")]
    public TMP_InputField messageInput;
    public TMP_Dropdown playerDropdown;
    public TMP_Text messageDisplay;

    private List<PlayerRef> otherPlayers = new List<PlayerRef>();

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            RefreshPlayerList();
        }
    }

    void RefreshPlayerList()
    {
        otherPlayers.Clear();
        playerDropdown.ClearOptions();

        foreach (PlayerRef p in Runner.ActivePlayers)
        {
            if (!p.Equals(Object.InputAuthority))
            {
                otherPlayers.Add(p);
                playerDropdown.options.Add(new TMP_Dropdown.OptionData($"Player {p.PlayerId}"));
            }
        }

        playerDropdown.RefreshShownValue();
    }

    public void OnSendMessageClicked()
    {
        if (messageInput.text.Length == 0 || otherPlayers.Count == 0)
            return;

        int selectedIndex = playerDropdown.value;
        PlayerRef targetPlayer = otherPlayers[selectedIndex];

        RPC_SendPrivateMessage(messageInput.text, targetPlayer);
        messageInput.text = "";
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_SendPrivateMessage(string msg, PlayerRef target)
    {
        if (Runner.LocalPlayer == target)
        {
            messageDisplay.text = $"Private message: {msg}";
        }
    }
}
