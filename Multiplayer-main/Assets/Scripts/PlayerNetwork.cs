using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Component.Transforming;
using UnityEngine;
using System.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private GameObject _visualModel;

    public readonly SyncVar<string> Nickname = new SyncVar<string>();
    public readonly SyncVar<int> HP = new SyncVar<int>(100);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);

    // UI events (invoked from SyncVar change handlers)
    public event System.Action<string> OnNicknameChangedUI;
    public event System.Action<int> OnHealthChangedUI;
    public event System.Action<bool> OnIsAliveChangedUI;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // Subscribe to SyncVar changes
        Nickname.OnChange += OnNicknameChanged;
        HP.OnChange += OnHealthChanged;
        IsAlive.OnChange += OnIsAliveChanged;

        // Only the local client sends its nickname
        if (base.Owner.IsLocalClient)
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);

        // Manually invoke initial UI update (value may already be set)
        OnNicknameChanged(Nickname.Value, Nickname.Value, false);
        OnHealthChanged(HP.Value, HP.Value, false);
        OnIsAliveChanged(IsAlive.Value, IsAlive.Value, false);
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
        Debug.Log($"[Server] Player {Owner.ClientId} -> \"{safeValue}\"");
    }

    private void OnNicknameChanged(string oldVal, string newVal, bool asServer)
    {
        OnNicknameChangedUI?.Invoke(newVal);
        if (Application.isEditor && !string.IsNullOrEmpty(newVal))
            gameObject.name = $"[{newVal}] Player";
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

        // Apply the new transform values first
        transform.position = position;
        transform.rotation = rotation;

        if (cc != null) cc.enabled = true;
        var nt = GetComponent<FishNet.Component.Transforming.NetworkTransform>();
        if (nt != null)
        {
            // Teleport without arguments – the NetworkTransform uses the current transform values
            nt.Teleport();
        }
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