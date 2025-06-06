using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using System.Threading.Tasks;

public class LobbyBrowser : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Lobby UI")]
    public TMP_InputField lobbyInput;
    public Button joinLobbyButton;
    public Transform sessionListContainer;
    public GameObject sessionButtonPrefab;

    [Header("Create Session UI")]
    public TMP_InputField sessionNameInput;
    public Button createSessionButton;

    [Header("Room (In-Game) UI")]
    public GameObject roomPanel;
    public Transform playerListContainer;
    public GameObject playerTextPrefab;

    [Header("Leave Room UI")]
    public Button leaveRoomButton;
    private NetworkRunner runner;
    private bool inLobby = false;
    private bool inRoom = false;

    async void Start()
    {
        runner = FindObjectOfType<NetworkRunner>() ?? gameObject.AddComponent<NetworkRunner>();

        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        joinLobbyButton.interactable = true;
        createSessionButton.interactable = false;
        leaveRoomButton.interactable = false;

        joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);
        createSessionButton.onClick.AddListener(OnCreateSessionClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

        sessionListContainer.gameObject.SetActive(false);
        roomPanel.SetActive(false);
    }

    async void OnJoinLobbyClicked()
    {
        string lobbyName = lobbyInput.text.Trim();
        if (string.IsNullOrEmpty(lobbyName))
            return;

        joinLobbyButton.interactable = false;

        var lobbyResult = await runner.JoinSessionLobby(SessionLobby.Custom, lobbyName);
        if (lobbyResult.Ok)
        {
            inLobby = true;
            Debug.Log($"Joined or created lobby '{lobbyName}'.");

            sessionListContainer.gameObject.SetActive(true);

            createSessionButton.interactable = true;
        }
        else
        {
            Debug.LogError($"Failed to join lobby '{lobbyName}': {lobbyResult.ShutdownReason}");
            joinLobbyButton.interactable = true;
        }
    }


    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessions)
    {
        if (!inLobby || inRoom)
            return;

        foreach (Transform child in sessionListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var s in sessions)
        {
            if (!s.IsOpen || !s.IsVisible)
                continue;

            GameObject entry = Instantiate(sessionButtonPrefab, sessionListContainer);
            TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
            label.text = $"{s.Name} ({s.PlayerCount}/{s.MaxPlayers})";

            Button btn = entry.GetComponent<Button>();
            btn.interactable = true;

            string sessionName = s.Name;
            btn.onClick.AddListener(() => JoinExistingSession(sessionName));
        }
    }


    async void OnCreateSessionClicked()
    {
        string newSessionName = sessionNameInput.text.Trim();
        if (string.IsNullOrEmpty(newSessionName))
        {
            Debug.LogWarning("Session name is empty. Type a name first.");
            return;
        }

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = newSessionName,
            IsOpen = true,
            IsVisible = true,
            CustomLobbyName = lobbyInput.text.Trim()
        };

        var result = await runner.StartGame(startArgs);
        if (result.Ok)
        {
            inRoom = true;
            Debug.Log($"Created and joined session '{newSessionName}'.");

            sessionListContainer.gameObject.SetActive(false);
            createSessionButton.interactable = false;

            roomPanel.SetActive(true);
            leaveRoomButton.interactable = true;

            UpdatePlayerList();
        }
        else
        {
            Debug.LogError($"Failed to create session '{newSessionName}': {result.ErrorMessage}");
        }
    }


    async void JoinExistingSession(string sessionName)
    {
        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            IsOpen = true,
            IsVisible = true,
            CustomLobbyName = lobbyInput.text.Trim()
        };

        var result = await runner.StartGame(startArgs);
        if (result.Ok)
        {
            inRoom = true;
            Debug.Log($"Joined session '{sessionName}'.");

            sessionListContainer.gameObject.SetActive(false);
            createSessionButton.interactable = false;

            roomPanel.SetActive(true);
            leaveRoomButton.interactable = true;

            UpdatePlayerList();
        }
        else
        {
            Debug.LogError($"Failed to join session '{sessionName}': {result.ErrorMessage}");
        }
    }

    void UpdatePlayerList()
    {
        foreach (Transform t in playerListContainer)
        {
            Destroy(t.gameObject);
        }

        foreach (PlayerRef p in runner.ActivePlayers)
        {
            GameObject txtObj = Instantiate(playerTextPrefab, playerListContainer);
            TMP_Text textComp = txtObj.GetComponent<TMP_Text>();
            textComp.text = $"Player {p.PlayerId}";
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (inRoom)
            UpdatePlayerList();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (inRoom)
            UpdatePlayerList();
    }


    async void OnLeaveRoomClicked()
    {
        await runner.Shutdown();

        ResetAll();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        ResetAll();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        ResetAll();
    }

    void ResetAll()
    {
        inLobby = false;
        inRoom = false;

        joinLobbyButton.interactable = true;
        createSessionButton.interactable = false;
        sessionListContainer.gameObject.SetActive(false);

        roomPanel.SetActive(false);
        leaveRoomButton.interactable = false;

        foreach (Transform t in sessionListContainer)
            Destroy(t.gameObject);
        foreach (Transform t in playerListContainer)
            Destroy(t.gameObject);
    }

    #region Fusion callbacks
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress addr, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    #endregion
}
