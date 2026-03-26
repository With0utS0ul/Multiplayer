using TMPro;
using Unity.Netcode;
using UnityEngine;
using System;
using Unity.Collections;

public class PlayerUI : NetworkBehaviour
{
    /*
    [Header("References")]
    [SerializeField] private PlayerNetwork _playerNetwork;

    [Header("UI Elements (Overlay Canvas)")]
    [SerializeField] private TMP_Text _nicknameText;   // Текст для ника
    [SerializeField] private TMP_Text _hpText;         // Текст для здоровья

    [Header("Settings")]
    [SerializeField] private bool _showOnlyForOwner = true; // Показывать UI только владельцу
    [SerializeField] private string _hpFormat = "HP: {0}";  // Формат отображения здоровья

    // ==================== Инициализация ====================

    public override void OnNetworkSpawn()
    {
        // Авто-поиск компонентов
        AssignReferences();

        // Если показываем только владельцу — скрываем UI у чужих игроков
        if (_showOnlyForOwner && !IsOwner)
        {
            SetUIVisible(false);
            return;
        }

        // Подписка на изменения сетевых переменных
        if (_playerNetwork != null)
        {
            _playerNetwork.Nickname.OnValueChanged += OnNicknameChanged;
            _playerNetwork.HP.OnValueChanged += OnHpChanged;
        }

        // Первичное обновление UI (чтобы не ждать первого события)
        RefreshUI();
    }

    public override void OnNetworkDespawn()
    {
        // ❗ Отписка — предотвращает утечки памяти
        if (_playerNetwork != null)
        {
            _playerNetwork.Nickname.OnValueChanged -= OnNicknameChanged;
            _playerNetwork.HP.OnValueChanged -= OnHpChanged;
        }
    }

    private void AssignReferences()
    {
        if (_playerNetwork == null)
            _playerNetwork = GetComponent<PlayerNetwork>();
    }

    // Обработчики NetworkVariable

    /// <summary>
    /// Вызывается на всех клиентах при изменении никнейма на сервере.
    /// </summary>
    private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        string nickname = newValue.ToString();

        if (_nicknameText != null)
            _nicknameText.text = nickname;

        // Для отладки: обновляем имя объекта в редакторе
        if (Application.isEditor && !string.IsNullOrEmpty(nickname))
            gameObject.name = $"[{nickname}] Player";
    }

    /// <summary>
    /// Вызывается на всех клиентах при изменении здоровья на сервере.
    /// </summary>
    private void OnHpChanged(int oldValue, int newValue)
    {
        if (_hpText != null)
            _hpText.text = string.Format(_hpFormat, newValue);

        //  визуальный эффект при низком здоровье
        if (newValue <= 30 && oldValue > 30 && _hpText != null)
            _hpText.color = Color.red;
        else if (newValue > 30 && _hpText != null)
            _hpText.color = Color.white; // Возврат к нормальному цвету
    }

    

    /// <summary>
    /// Полное обновление UI (вызывается при спавне или вручную).
    /// </summary>
    public void RefreshUI()
    {
        if (_playerNetwork == null) return;

        // Принудительно вызываем обработчики с текущими значениями
        OnNicknameChanged(default, _playerNetwork.Nickname.Value);
        OnHpChanged(0, _playerNetwork.HP.Value);
    }

    /// <summary>
    /// Показать/скрыть весь UI этого игрока.
    /// </summary>
    public void SetUIVisible(bool visible)
    {
        if (_nicknameText != null) _nicknameText.gameObject.SetActive(visible);
        if (_hpText != null) _hpText.gameObject.SetActive(visible);
    }

    // ==================== Отладка ====================

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Валидация в редакторе
        _hpFormat = string.IsNullOrEmpty(_hpFormat) ? "HP: {0}" : _hpFormat;
    }
#endif*/

    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private Transform cameraFollowPoint;

    public override void OnNetworkSpawn()
    {
        // 1. Подписка на события (для всех)
        if (_playerNetwork != null)
        {
            _playerNetwork.Nickname.OnValueChanged += OnNicknameChanged;
            _playerNetwork.HP.OnValueChanged += OnHpChanged;
        }

        // 2. ⚠️ ПЕРВИЧНОЕ ОБНОВЛЕНИЕ UI (ДЛЯ ВСЕХ КЛИЕНТОВ)
        // Это заполнит текст актуальными значениями сразу после спавна,
        // даже если события изменения ещё не было.
        RefreshUI();

        // 3. Логика, которая нужна только владельцу (камера, ввод)
        if (!IsOwner) return;

        Camerapoint();
    }
    public void RefreshUI()
    {
        if (_playerNetwork == null) return;

        // Имитируем вызов событий с текущими данными
        OnNicknameChanged(default, _playerNetwork.Nickname.Value);
        OnHpChanged(0, _playerNetwork.HP.Value);
    }

    public override void OnNetworkDespawn()
    {
        // Отписка обязательна, чтобы не оставлять "висячие" обработчики.
        _playerNetwork.Nickname.OnValueChanged -= OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged -= OnHpChanged;
    }

    private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        _nicknameText.text = newValue.ToString();
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        _hpText.text = $"HP: {newValue}";
    }

    private void Camerapoint()
    {
        Camera cam = Camera.main;
        cam.transform.SetParent(cameraFollowPoint);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;
    }
    
}