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

    /*
    public void ReturnToMainMenu()
    {
        if (GameManager.Instance != null && GameManager.Instance.Object != null && GameManager.Instance.Object.HasStateAuthority)
        {
            Debug.Log("[GameOverManager] Host requested return to lobby. Calling GameManager.RPC_ReturnToLobby()");
            GameManager.Instance.RPC_ReturnToLobby();
            return;
        }

        Debug.Log("[GameOverManager] Non-host or GameManager missing, not good, but shall perform a local shutdown andd load.");
        if (Runner != null && Runner.IsRunning)
        {
            try
            {
                Runner.Shutdown();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameOverManager] Runner.Shutdown log: {ex}");
            }
        }

        SceneManager.LoadScene("SampleScene"); // Replace with main menu scene name if we change it, DO NOT FORGET!
    }
    */
    public void ReturnToMainMenu()
    {
        // If GameManager exists and this peer is host, ask it to return everyone, nice and careful, they are frigile.
        if (GameManager.Instance != null && GameManager.Instance.Object != null && GameManager.Instance.Object.HasStateAuthority)
        {
            Debug.Log("[GameOverManager] Host requested return to lobby via GameManager RPC.");
            GameManager.Instance.RPC_ReturnToLobby();
            return;
        }

        // I have to perform a "local" safe return using Hermes.
        Debug.Log("[GameOverManager] there is no host and No GameManager what do we do?! answer is performing local return to lobby!");
        Hermes.StartReturnToLobbySequence("SampleScene"); // Replace with main menu scene name if we change it, DO NOT FORGET!
    }


}
