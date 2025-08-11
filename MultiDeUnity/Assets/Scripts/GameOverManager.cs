using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverManager : NetworkBehaviour
{
    public GameObject gameOverPanel;

    public void TriggerGameOver()
    {
        if (Runner.IsServer)
        {
            RPC_ShowGameOverUI();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowGameOverUI()
    {
        gameOverPanel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        if (Runner)
        {
            Runner.Shutdown();
        }

        SceneManager.LoadScene("SampleScene"); // Replace with main menu scene name if we change it!
    }
}
