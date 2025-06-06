using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class LobblyManager : MonoBehaviour
{

    [SerializeField] NetworkRunner networkRunner;
    [SerializeField] GameObject StartButton;

    public void StartSession()
    {
        networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = gameObject.name,
            OnGameStarted = OnGameStarted

        });
    }


    private void OnGameStarted(NetworkRunner obj)
    {
        Debug.Log("Game Started");
        StartButton.SetActive(false);

    }
}
