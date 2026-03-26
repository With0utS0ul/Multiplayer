using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System;
using UnityEditor.MemoryProfiler;

public class PlayerNetwork : NetworkBehaviour
{
    // Сетевые переменные 


    /// Никнейм: читают все, пишет только сервер.
    /// FixedString32Bytes — сетевой-сериализуемый тип для строк.

    public readonly NetworkVariable<FixedString32Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


    /// Здоровье: читают все, пишет только сервер.
    /// Стартовое значение: 100.

    public readonly NetworkVariable<int> HP = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // События для UI


    /// Вызывается на всех клиентах при изменении никнейма.

    public event Action<string> OnNicknameChanged;


    /// Вызывается на всех клиентах при изменении здоровья.

    public event Action<int> OnHealthChanged;

    // Инициализация 
    public override void OnNetworkSpawn()
    {
        // Подписка на изменения сетевых переменных
        Nickname.OnValueChanged += OnNicknameValueChanged;
        HP.OnValueChanged += OnHealthValueChanged;

        // Если объект уже заспавнен — сразу уведомляем подписчиков
        if (IsSpawned)
        {
            OnNicknameValueChanged(default, Nickname.Value);
            OnHealthValueChanged(100, HP.Value);
        }

        // Только локальный владелец отправляет свой ник на сервер
        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    public override void OnNetworkDespawn()
    {
        // Отписка от событий для предотвращения утечек памяти
        Nickname.OnValueChanged -= OnNicknameValueChanged;
        HP.OnValueChanged -= OnHealthValueChanged;
    }

    //  ServerRpc: отправка ника на сервер 


    /// Клиент вызывает этот метод, чтобы передать свой никнейм серверу.
    /// RequireOwnership = false позволяет вызывать даже до полного спавна.

    [ServerRpc(RequireOwnership = false)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        // ⚠️ Сервер — единственный источник истины: нормализуем входные данные
        string rawValue = nickname ?? string.Empty;
        string safeValue = string.IsNullOrWhiteSpace(rawValue)
            ? $"Player_{OwnerClientId}"
            : rawValue.Trim();

        // Ограничиваем длину (на случай, если клиент обойдёт проверку)
        if (safeValue.Length > 32)
            safeValue = safeValue.Substring(0, 32);

        // Записываем в NetworkVariable — изменение автоматически уйдёт всем клиентам
        Nickname.Value = safeValue;

        Debug.Log($"[Server] Player {OwnerClientId} registered as \"{safeValue}\"");
    }

    // Обработчики изменений 

    private void OnNicknameValueChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        // Конвертируем FixedString32Bytes → string для удобства
        string currentString = current.ToString();
        OnNicknameChanged?.Invoke(currentString);

        // Обновляем имя объекта в редакторе для удобной отладки
        if (Application.isEditor && !string.IsNullOrEmpty(currentString))
            gameObject.name = $"[{currentString}] Player";
    }

    private void OnHealthValueChanged(int previous, int current)
    {
        OnHealthChanged?.Invoke(current);

        // Логика при смерти (срабатывает только при переходе через 0)
        if (current <= 0 && previous > 0)
        {
            OnPlayerDefeatedServerRpc();
        }
    }

    // ServerRpc: обработка смерти

    [ServerRpc(RequireOwnership = false)]
    private void OnPlayerDefeatedServerRpc()
    {
        // Здесь можно: начислить очки убийце, заспавнить эффект, запланировать возрождение
        Debug.Log($"[Server] Player {Nickname.Value} (ID: {OwnerClientId}) defeated!");

        // Пример: не даём здоровью уйти ниже 0 (дополнительная страховка)
        if (HP.Value > 0)
            HP.Value = 0;
    }

    //  Публичные методы для чтения 


    /// Безопасное получение никнейма (работает с любого клиента).

    public string GetNickname() => Nickname.Value.ToString();


    /// Безопасное получение текущего здоровья (работает с любого клиента).

    public int GetCurrentHealth() => HP.Value;


    /// Проверка: жив ли игрок?

    public bool IsAlive() => HP.Value > 0;
}