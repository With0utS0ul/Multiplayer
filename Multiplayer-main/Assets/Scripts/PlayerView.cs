using TMPro;
using FishNet.Object;
using UnityEngine;
using System.Collections;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private PlayerShooting _playerShooting;

    [Header("World Space UI")]
    [SerializeField] private TMP_Text _worldNicknameText;
    [SerializeField] private TMP_Text _worldHpText;

    private TMP_Text _screenNicknameText;
    private TMP_Text _screenHpText;
    private TMP_Text _screenAmmoText;
    private TMP_Text _screenRespawnTimerText;

    private Coroutine _respawnCoroutine;

    private void Awake()
    {
        if (_playerNetwork == null) _playerNetwork = GetComponent<PlayerNetwork>();
        if (_playerShooting == null) _playerShooting = GetComponent<PlayerShooting>();
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // Get references from HUDManager
        if (HUDManager.Instance != null)
        {
            _screenNicknameText = HUDManager.Instance.NicknameText;
            _screenHpText = HUDManager.Instance.HPText;
            _screenAmmoText = HUDManager.Instance.AmmoText;
            _screenRespawnTimerText = HUDManager.Instance.RespawnTimerText;
        }
        else Debug.LogError("HUDManager not found!");

        // Subscribe to UI events from PlayerNetwork and PlayerShooting
        if (_playerNetwork != null)
        {
            _playerNetwork.OnNicknameChangedUI += OnNicknameChanged;
            _playerNetwork.OnHealthChangedUI += OnHpChanged;
            _playerNetwork.OnIsAliveChangedUI += OnIsAliveChanged;
        }
        if (_playerShooting != null)
            _playerShooting.OnAmmoChangedUI += OnAmmoChanged;

        // Initial UI update
        OnNicknameChanged(_playerNetwork.Nickname.Value);
        OnHpChanged(_playerNetwork.HP.Value);
        if (_playerShooting != null)
            OnAmmoChanged(_playerShooting.CurrentAmmo.Value);

        if (_screenRespawnTimerText != null)
            _screenRespawnTimerText.gameObject.SetActive(false);

        // For non-local players, clear screen HUD references (they should not affect local HUD)
        if (!base.Owner.IsLocalClient)
        {
            _screenNicknameText = null;
            _screenHpText = null;
            _screenAmmoText = null;
            _screenRespawnTimerText = null;
        }
    }

    public override void OnStopNetwork()
    {
        if (_playerNetwork != null)
        {
            _playerNetwork.OnNicknameChangedUI -= OnNicknameChanged;
            _playerNetwork.OnHealthChangedUI -= OnHpChanged;
            _playerNetwork.OnIsAliveChangedUI -= OnIsAliveChanged;
        }
        if (_playerShooting != null)
            _playerShooting.OnAmmoChangedUI -= OnAmmoChanged;
    }

    private void OnNicknameChanged(string nick)
    {
        if (_worldNicknameText != null) _worldNicknameText.text = nick;
        if (_screenNicknameText != null) _screenNicknameText.text = nick;
    }

    private void OnHpChanged(int hp)
    {
        string hpStr = $"HP: {hp}";
        if (_worldHpText != null) _worldHpText.text = hpStr;
        if (_screenHpText != null) _screenHpText.text = hpStr;
    }

    private void OnAmmoChanged(int ammo)
    {
        if (_screenAmmoText != null)
            _screenAmmoText.text = $"Ammo: {ammo}";
    }

    private void OnIsAliveChanged(bool isAlive)
    {
        if (!base.Owner.IsLocalClient) return;
        if (!isAlive) StartRespawnTimer();
        else StopRespawnTimer();
    }

    private void StartRespawnTimer()
    {
        if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
        _respawnCoroutine = StartCoroutine(RespawnTimerCoroutine());
    }

    private void StopRespawnTimer()
    {
        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = null;
        }
        if (_screenRespawnTimerText != null)
            _screenRespawnTimerText.gameObject.SetActive(false);
    }

    private IEnumerator RespawnTimerCoroutine()
    {
        float timer = 3f;
        while (timer > 0f)
        {
            if (_screenRespawnTimerText != null)
            {
                _screenRespawnTimerText.gameObject.SetActive(true);
                _screenRespawnTimerText.text = $"Respawning in {timer:F1}";
            }
            Debug.Log($"[RespawnTimer] timer = {timer}"); // для отладки
            timer -= Time.deltaTime;
            yield return null;
        }
        if (_screenRespawnTimerText != null)
            _screenRespawnTimerText.text = "Respawning...";
    }
}