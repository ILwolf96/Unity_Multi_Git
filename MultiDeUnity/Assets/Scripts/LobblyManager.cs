using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class LobbyBrowser : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Lobby UI")]
    [SerializeField] private TMP_InputField lobbyInput = null;
    [SerializeField] private Button joinLobbyButton = null;
    [SerializeField] private Transform sessionListContainer = null;
    [SerializeField] private GameObject sessionButtonPrefab = null;

    [Header("Create Session UI")]
    [SerializeField] private TMP_InputField sessionNameInput = null;
    [SerializeField] private Button createSessionButton = null;
    [SerializeField] private bool isSessionVisibleInLobby = true;

    [Header("Room (In-Game) UI")]
    [SerializeField] private GameObject roomPanel = null;
    [SerializeField] private Transform playerListContainer = null;
    [SerializeField] private GameObject playerTextPrefab = null;

    [Header("Leave Room UI")]
    [SerializeField] private Button leaveRoomButton = null;

    [Header("Scene Management")]
    [SerializeField] private SceneRef gameScene;

    private NetworkRunner _runner;
    private bool _inLobby = false;
    private bool _inRoom = false;

    private async void Start()
    {
        _runner = FindObjectOfType<NetworkRunner>();
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            if (_runner.GetComponent<NetworkSceneManagerDefault>() == null)
            {
                _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }
        }

        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        joinLobbyButton.interactable = true;
        createSessionButton.interactable = false;
        leaveRoomButton.interactable = false;

        joinLobbyButton.onClick.AddListener(JoinLobbyAsync);
        createSessionButton.onClick.AddListener(CreateSessionAsync);
        leaveRoomButton.onClick.AddListener(LeaveRoomAsync);

        sessionListContainer.gameObject.SetActive(false);
        roomPanel.SetActive(false);
    }

    private async void JoinLobbyAsync()
    {
        string lobbyName = lobbyInput.text.Trim();
        if (string.IsNullOrEmpty(lobbyName))
        {
            Debug.LogWarning("Lobby name cannot be empty.");
            return;
        }

        joinLobbyButton.interactable = false;

        if (_runner.IsRunning)
        {
            await _runner.Shutdown();
            _runner = gameObject.AddComponent<NetworkRunner>();
            if (_runner.GetComponent<NetworkSceneManagerDefault>() == null)
            {
                _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
        }

        var lobbyResult = await _runner.JoinSessionLobby(SessionLobby.Custom, lobbyName);
        if (lobbyResult.Ok)
        {
            _inLobby = true;
            Debug.Log($"Joined lobby '{lobbyName}'.");

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
        if (!_inLobby || _inRoom)
            return;

        foreach (Transform child in sessionListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var session in sessions)
        {
            if (!session.IsOpen || !session.IsVisible || session.PlayerCount >= session.MaxPlayers)
                continue;

            GameObject entry = Instantiate(sessionButtonPrefab, sessionListContainer);
            TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
            label.text = $"{session.Name} ({session.PlayerCount}/{session.MaxPlayers})";

            Button btn = entry.GetComponent<Button>();
            btn.interactable = true;

            string sessionNameCopy = session.Name; 
            btn.onClick.AddListener(() => JoinExistingSessionAsync(sessionNameCopy));
        }
    }

    private async void CreateSessionAsync()
    {
        string newSessionName = sessionNameInput.text.Trim();
        if (string.IsNullOrEmpty(newSessionName))
        {
            Debug.LogWarning("Session name cannot be empty.");
            return;
        }

        if (_runner.IsRunning)
        {
            await _runner.Shutdown();
            _runner = gameObject.AddComponent<NetworkRunner>();
            if (_runner.GetComponent<NetworkSceneManagerDefault>() == null)
            {
                _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
        }

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host, 
            SessionName = newSessionName,
            Scene = gameScene, 
            IsOpen = true,
            IsVisible = isSessionVisibleInLobby,
            CustomLobbyName = lobbyInput.text.Trim()
        };

        var result = await _runner.StartGame(startArgs);
        if (result.Ok)
        {
            _inRoom = true;
            Debug.Log($"Created and joined session '{newSessionName}' as HOST.");

            sessionListContainer.gameObject.SetActive(false);
            createSessionButton.interactable = false;
            joinLobbyButton.interactable = false;

            roomPanel.SetActive(true);
            leaveRoomButton.interactable = true;

        }
        else
        {
            Debug.LogError($"Failed to create session '{newSessionName}': {result.ErrorMessage}");
            createSessionButton.interactable = true;
        }
    }

    private async void JoinExistingSessionAsync(string sessionName)
    {
        if (_runner.IsRunning)
        {
            await _runner.Shutdown();
            _runner = gameObject.AddComponent<NetworkRunner>();
            if (_runner.GetComponent<NetworkSceneManagerDefault>() == null)
            {
                _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
        }

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client, 
            SessionName = sessionName,
            Scene = gameScene, 
            IsOpen = true, 
            IsVisible = true, 
            CustomLobbyName = lobbyInput.text.Trim()
        };

        var result = await _runner.StartGame(startArgs);
        if (result.Ok)
        {
            _inRoom = true;
            Debug.Log($"Joined session '{sessionName}' as CLIENT.");

            sessionListContainer.gameObject.SetActive(false);
            createSessionButton.interactable = false;
            joinLobbyButton.interactable = false; 

            roomPanel.SetActive(true);
            leaveRoomButton.interactable = true;

        }
        else
        {
            Debug.LogError($"Failed to join session '{sessionName}': {result.ErrorMessage}");
            sessionListContainer.gameObject.SetActive(true);
            createSessionButton.interactable = true;
            joinLobbyButton.interactable = true;
        }
    }

    private void UpdatePlayerList()
    {
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        if (_runner != null && _runner.IsRunning)
        {
            foreach (PlayerRef player in _runner.ActivePlayers)
            {
                GameObject playerText = Instantiate(playerTextPrefab, playerListContainer);
                TMP_Text textComponent = playerText.GetComponent<TMP_Text>();
                textComponent.text = $"Player {player.PlayerId} {(player == _runner.LocalPlayer ? "(YOU)" : "")}";

                if (_runner.GetPlayerObject(player) != null)
                {
                    textComponent.text += " (Ready)";
                }
            }
        }
    }

    private async void LeaveRoomAsync()
    {
        if (_runner != null && _runner.IsRunning)
        {
            await _runner.Shutdown();
        }
        ResetUI();
    }

    #region INetworkRunnerCallbacks

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} joined the session.");
        if (_inRoom) 
            UpdatePlayerList();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} left the session.");
        if (_inRoom) 
            UpdatePlayerList();
        if (runner.IsServer && CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnPlayerLeft(runner, player);
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to server.");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"Runner Shutdown: {reason}");
        ResetUI();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress addr, NetConnectFailedReason reason)
    {
        Debug.LogError($"Connection failed to {addr}: {reason}");
        ResetUI(); 
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected from server: {reason}");
        ResetUI();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log($"Scene loaded successfully by NetworkRunner.");
        roomPanel.SetActive(true);
        leaveRoomButton.interactable = true;
        UpdatePlayerList();
    }

    private void ResetUI()
    {
        _inLobby = false;
        _inRoom = false;

        joinLobbyButton.interactable = true;
        createSessionButton.interactable = true; 
        sessionListContainer.gameObject.SetActive(false);

        roomPanel.SetActive(false);
        leaveRoomButton.interactable = false;

        foreach (Transform child in sessionListContainer)
            Destroy(child.gameObject);

        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);
    }

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

    #endregion
}