using Unity.Netcode;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _attackRange = 3f;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private KeyCode _attackKey = KeyCode.Mouse0; // ЛКМ по умолчанию

    [Header("References")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private PlayerNetwork _playerNetwork;

    private float _lastAttackTime;
    [SerializeField] private float _attackCooldown = 0.5f;

    private void Update()
    {
        // Атаку может инициировать только локальный владелец объекта
        if (!IsOwner) return;

        // Обработка ввода: клавиша 
        if (Input.GetKeyDown(_attackKey) && Time.time - _lastAttackTime >= _attackCooldown)
        {
            TryAttack();
            _lastAttackTime = Time.time;
        }
    }

    
    // Вызывается по нажатию кнопки атаки 
    
    public void TryAttack()
    {
        if (!IsOwner || _playerNetwork == null) return;

        // Raycast для поиска цели
        if (FindTarget(out PlayerNetwork target))
        {
            // Отправляем запрос на сервер через ServerRpc
            DealDamageServerRpc(target.NetworkObjectId, _damage);
            
        }
    }

    
    // Простая логика поиска цели через Raycast
    
    private bool FindTarget(out PlayerNetwork target)
    {
        target = null;

        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
            if (_playerCamera == null) return false;
        }

        Ray ray = _playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, _attackRange, _targetLayer))
        {
            target = hit.collider.GetComponentInParent<PlayerNetwork>();
            return target != null && target.IsSpawned;
        }

        return false;
    }

   
    // ServerRpc: выполняется ТОЛЬКО на сервере/хосте
    
    [ServerRpc]
    private void DealDamageServerRpc(ulong targetObjectId, int damage)
    {
        // 1. Проверяем существование цели среди заспавненных объектов
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject targetObject))
        {
            Debug.LogWarning($"[ServerRpc] Target object {targetObjectId} not found!");
            return;
        }

        // 2. Получаем компонент здоровья цели
        PlayerNetwork targetPlayer = targetObject.GetComponent<PlayerNetwork>();
        if (targetPlayer == null)
        {
            Debug.LogWarning($"[ServerRpc] Target has no PlayerNetwork component!");
            return;
        }

        // 3. Запрещаем атаковать самого себя
        if (targetPlayer == _playerNetwork)
        {
            Debug.Log($"[ServerRpc] Player cannot attack themselves!");
            return;
        }

        // 4. проверка дистанции на сервере (защита от читеров)
        float distance = Vector3.Distance(transform.position, targetObject.transform.position);
        if (distance > _attackRange)
        {
            Debug.LogWarning($"[ServerRpc] Target too far! Distance: {distance:F2}");
            return;
        }

        // 5. Применяем урон: ограничиваем снизу нулём
        int currentHp = targetPlayer.HP.Value;
        int newHp = Mathf.Max(0, currentHp - damage);
        targetPlayer.HP.Value = newHp; // NetworkVariable автоматически синхронизирует изменение

        Debug.Log($"[ServerRpc] {gameObject.name} dealt {damage} damage to {targetPlayer.gameObject.name}. HP: {currentHp} → {newHp}");
    }
}