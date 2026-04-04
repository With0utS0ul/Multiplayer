using TMPro;
using Unity.Netcode;
using UnityEngine;
using System;
using Unity.Collections;
using System.Collections;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private PlayerShooting _playerShooting;

    // --- Надголовный UI (World Space) - только ник и HP ---
    [Header("World Space UI (over head)")]
    [SerializeField] private TMP_Text _worldNicknameText;
    [SerializeField] private TMP_Text _worldHpText;

    // --- Экранный HUD (через HUDManager) ---
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

    public override void OnNetworkSpawn()
    {
        // Получаем ссылки на экранный HUD
        if (HUDManager.Instance != null)
        {
            _screenNicknameText = HUDManager.Instance.NicknameText;
            _screenHpText = HUDManager.Instance.HPText;
            _screenAmmoText = HUDManager.Instance.AmmoText;
            _screenRespawnTimerText = HUDManager.Instance.RespawnTimerText;
        }
        else
        {
            Debug.LogError("HUDManager не найден!");
        }

        // Подписки на события (для всех игроков, чтобы обновлять надголовный UI)
        if (_playerNetwork != null)
        {
            _playerNetwork.Nickname.OnValueChanged += OnNicknameChanged;
            _playerNetwork.HP.OnValueChanged += OnHpChanged;
            _playerNetwork.IsAlive.OnValueChanged += OnIsAliveChanged;
        }

        if (_playerShooting != null)
        {
            _playerShooting.OnAmmoChanged += OnAmmoChanged;
        }

        // Первичное обновление
        OnNicknameChanged(default, _playerNetwork.Nickname.Value);
        OnHpChanged(0, _playerNetwork.HP.Value);
        if (_playerShooting != null)
            OnAmmoChanged(_playerShooting.CurrentAmmo.Value);

        // Скрыть таймер на экране
        if (_screenRespawnTimerText != null)
            _screenRespawnTimerText.gameObject.SetActive(false);

        // Если это не локальный игрок – отключаем обновление экранного HUD
        if (!IsOwner)
        {
            _screenNicknameText = null;
            _screenHpText = null;
            _screenAmmoText = null;
            _screenRespawnTimerText = null;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (_playerNetwork != null)
        {
            _playerNetwork.Nickname.OnValueChanged -= OnNicknameChanged;
            _playerNetwork.HP.OnValueChanged -= OnHpChanged;
            _playerNetwork.IsAlive.OnValueChanged -= OnIsAliveChanged;
        }
        if (_playerShooting != null)
            _playerShooting.OnAmmoChanged -= OnAmmoChanged;
    }

    private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        string nick = newValue.ToString();
        if (_worldNicknameText != null) _worldNicknameText.text = nick;
        if (_screenNicknameText != null) _screenNicknameText.text = nick;
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        string hpStr = $"HP: {newValue}";
        if (_worldHpText != null) _worldHpText.text = hpStr;
        if (_screenHpText != null) _screenHpText.text = hpStr;
    }

    private void OnAmmoChanged(int newAmmo)
    {
        Debug.Log($"[PlayerView] OnAmmoChanged called with {newAmmo}, screenAmmoText null? {_screenAmmoText == null}");
        if (_screenAmmoText != null)
            _screenAmmoText.text = $"Ammo: {newAmmo}";
    }

    private void OnIsAliveChanged(bool previous, bool isAlive)
    {
        if (!IsOwner) return;

        if (!isAlive)
            StartRespawnTimer();
        else
            StopRespawnTimer();
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
                _screenRespawnTimerText.text = $"Respawn in {timer:F1}";
            }
            timer -= Time.deltaTime;
            yield return null;
        }
        if (_screenRespawnTimerText != null)
            _screenRespawnTimerText.text = "Respawning...";
    }
}