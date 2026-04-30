using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;

public class ConnectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private TMP_InputField _nicknameInput;

    public static string PlayerNickname { get; private set; } = "Player";

    private NetworkManager _netManager;

    private void Awake()
    {
        _netManager = InstanceFinder.NetworkManager;
        if (_netManager == null)
        {
            Debug.LogError("FishNet NetworkManager not found!");
            return;
        }
        _netManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
    }

    private void OnEnable()
    {
        if (_hostButton != null) _hostButton.onClick.AddListener(OnHostClicked);
        if (_joinButton != null) _joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnDisable()
    {
        if (_hostButton != null) _hostButton.onClick.RemoveListener(OnHostClicked);
        if (_joinButton != null) _joinButton.onClick.RemoveListener(OnJoinClicked);
    }

    private void OnDestroy()
    {
        if (_netManager != null && _netManager.ClientManager != null)
            _netManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
            UpdateStatus($"Connected as \"{PlayerNickname}\"");
        else if (args.ConnectionState == LocalConnectionState.Stopped)
            UpdateStatus("Disconnected");
    }

    private void OnHostClicked()
    {
        SaveNickname();
        _netManager.ServerManager.StartConnection();
        _netManager.ClientManager.StartConnection();
        UpdateStatus($"Hosting as \"{PlayerNickname}\"...");
    }

    private void OnJoinClicked()
    {
        SaveNickname();
        _netManager.ClientManager.StartConnection();
        UpdateStatus($"Connecting as \"{PlayerNickname}\"...");
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }

    private void UpdateStatus(string message)
    {
        if (_statusText != null)
            _statusText.text = message;
        Debug.Log($"[ConnectionUI] {message}");
    }

    // Public methods for external calls
    public void StartAsHost() => OnHostClicked();
    public void StartAsClient() => OnJoinClicked();
    public void UpdateNickname(string newNickname)
    {
        PlayerNickname = string.IsNullOrWhiteSpace(newNickname) ? "Player" : newNickname.Trim();
        if (_nicknameInput != null) _nicknameInput.text = PlayerNickname;
    }
}