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

        SceneManager.LoadScene("MainMenuScene"); // Replace with your actual main menu scene name
    }
}
