using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Component.Transforming;
using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private GameObject _visualModel;
    [SerializeField] private TMP_Text _worldNicknameText;

    public readonly SyncVar<string> Nickname = new SyncVar<string>();
    public readonly SyncVar<int> HP = new SyncVar<int>(100);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);

    public event System.Action<string> OnNicknameChangedUI;
    public event System.Action<int> OnHealthChangedUI;
    public event System.Action<bool> OnIsAliveChangedUI;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        Nickname.OnChange += OnNicknameChanged;
        HP.OnChange += OnHealthChanged;
        IsAlive.OnChange += OnIsAliveChanged;

        UpdateNicknameUI(Nickname.Value); // первичное обновление (значение может быть пустым)

        if (base.Owner.IsLocalClient)
        {
            // Искусственная задержка, чтобы клиент успел инициализироваться
            StartCoroutine(DelayedNicknameSubmission());
        }

        OnHealthChanged(HP.Value, HP.Value, false);
        OnIsAliveChanged(IsAlive.Value, IsAlive.Value, false);
    }

    private IEnumerator DelayedNicknameSubmission()
    {
        yield return new WaitForSeconds(0.1f); // ждём 0.1 секунды
        if (base.Owner.IsLocalClient)
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
    }

    public override void OnStopNetwork()
    {
        Nickname.OnChange -= OnNicknameChanged;
        HP.OnChange -= OnHealthChanged;
        IsAlive.OnChange -= OnIsAliveChanged;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname)
            ? $"Player_{Owner.ClientId}"
            : nickname.Trim();
        if (safeValue.Length > 32) safeValue = safeValue.Substring(0, 32);
        Nickname.Value = safeValue;
    }

    private void OnNicknameChanged(string oldVal, string newVal, bool asServer)
    {
        UpdateNicknameUI(newVal);
    }

    private void UpdateNicknameUI(string nickname)
    {
        OnNicknameChangedUI?.Invoke(nickname);
        if (_worldNicknameText != null)
            _worldNicknameText.text = nickname;
        else
            Debug.LogWarning("World nickname text not assigned in PlayerNetwork!");

        if (Application.isEditor && !string.IsNullOrEmpty(nickname))
            gameObject.name = $"[{nickname}] Player";
    }

    private void OnHealthChanged(int oldVal, int newVal, bool asServer)
    {
        OnHealthChangedUI?.Invoke(newVal);
        if (asServer && newVal <= 0 && IsAlive.Value)
        {
            Debug.Log($"[Server] {Nickname.Value} died");
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        if (!base.IsServerStarted) yield break;
        yield return new WaitForSeconds(3f);
        Transform spawnPoint = SpawnManager.Instance?.GetRandomSpawnPoint();
        if (spawnPoint == null) yield break;

        TeleportObserversRpc(spawnPoint.position, spawnPoint.rotation);
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        HP.Value = 100;
        if (TryGetComponent(out PlayerShooting shooting))
            shooting.CurrentAmmo.Value = shooting._maxAmmo;
        IsAlive.Value = true;
    }

    [ObserversRpc]
    private void TeleportObserversRpc(Vector3 position, Quaternion rotation)
    {
        if (!base.Owner.IsLocalClient) return;
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        if (cc != null) cc.enabled = true;
        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.Teleport();
    }

    private void OnIsAliveChanged(bool oldVal, bool newVal, bool asServer)
    {
        OnIsAliveChangedUI?.Invoke(newVal);
        if (_visualModel != null)
            _visualModel.SetActive(newVal);
        else
        {
            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers) r.enabled = newVal;
        }
    }
}