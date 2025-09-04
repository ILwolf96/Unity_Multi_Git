using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

// Fun Fact, I called it Hermes cuz Wiki says 
//Hermes is the Greek god of travel, communication, and boundaries, is often depicted as a guide for souls.
//He leads people to their rightful places, both in life and after death, such as guiding souls to the Underworld.
//so it felt quite fitting to name it such cuz it shall return the players back to the Lobby after the session ends.

public class Hermes : MonoBehaviour
{
    private static Hermes _instance;

    private static bool _isReturning = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static void StartReturnToLobbySequence(string lobbySceneName = "SampleScene") // just to be safe, you know, I know I can just put the name of the scene, but whatever
    {
        if (_instance == null)
        {
            var go = new GameObject("[Hermes]");
            _instance = go.AddComponent<Hermes>();
            DontDestroyOnLoad(go);
        }

        if (_isReturning)
        {
            Debug.Log("[Hermes] Return already in progress.");
            return;
        }

        _isReturning = true;
        _instance.StartCoroutine(_instance.ReturnCoroutine(lobbySceneName));
    }

    private IEnumerator ReturnCoroutine(string lobbySceneName)
    {
        Debug.Log("[Hermes] Return sequence started.");

        yield return null;

        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null && runner.IsRunning)
        {
            if (runner.IsServer)
            {
                yield return new WaitForSeconds(0.15f);
            }

            Debug.Log("[Hermes] Shutting down NetworkRunner.");
            try
            {
                runner.Shutdown();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Hermes] Runner.Shutdown threw: " + ex);
            }

            float timeout = 1f;
            while (timeout > 0f && runner != null && runner.IsRunning)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.Log("[Hermes] Destroying the NetworkRunner GameObject.");
            //Destroy(runner.gameObject); // You know, based on the lore, he also sent them to death, so, the name is still fitting
        }
        else
        {
            Debug.Log("[Hermes] No running NetworkRunner found, my job is finished.");
        }

        yield return null;

        Debug.Log("[Hermes] Loading lobby scene: " + lobbySceneName);
        SceneManager.LoadScene(lobbySceneName);

        _isReturning = false;
    }
}
