using FishNet.Object;
using FishNet.Component.Prediction;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 40;
    private PickupManager _manager;
    private Vector3 _spawnPosition;
    private NetworkTrigger _networkTrigger;
    private bool _isUsed = false; // Флаг, чтобы аптечка не лечила дважды

    private void Awake()
    {
        if (!TryGetComponent(out _networkTrigger))
        {
            Debug.LogError("NetworkTrigger component is missing on the HealthPickup prefab!");
            return;
        }
        _networkTrigger.OnEnter += HandleTriggerEnter;
    }

    private void HandleTriggerEnter(Collider other, uint clientId)
    {
        // Только сервер обрабатывает лечение
        if (!base.IsServerStarted) return;
        // Если аптечка уже использована, игнорируем
        if (_isUsed) return;

        ApplyHeal(other);
    }

    private void ApplyHeal(Collider other)
    {
        var player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;
        if (!player.IsAlive.Value) return;
        if (player.HP.Value >= 100) return;

        player.HP.Value = Mathf.Min(100, player.HP.Value + _healAmount);
        Debug.Log($"Player healed! New HP: {player.HP.Value}");

        _isUsed = true; // Помечаем как использованную
        _manager.OnPickedUp(_spawnPosition);
        base.Despawn(DespawnType.Destroy);
    }

    public void Init(PickupManager manager)
    {
        _manager = manager;
        _spawnPosition = transform.position;
    }

    private void OnDestroy()
    {
        if (_networkTrigger != null)
            _networkTrigger.OnEnter -= HandleTriggerEnter;
    }
}