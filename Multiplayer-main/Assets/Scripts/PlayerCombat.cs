using FishNet.Object;
using FishNet.Managing;
using UnityEngine;
using FishNet;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _attackRange = 3f;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private KeyCode _attackKey = KeyCode.Mouse0;

    [Header("References")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private PlayerNetwork _playerNetwork;

    private float _lastAttackTime;
    [SerializeField] private float _attackCooldown = 0.5f;

    private void Update()
    {
        if (!base.Owner.IsLocalClient) return;
        if (Input.GetKeyDown(_attackKey) && Time.time - _lastAttackTime >= _attackCooldown)
        {
            TryAttack();
            _lastAttackTime = Time.time;
        }
    }

    private void TryAttack()
    {
        if (!base.Owner.IsLocalClient || _playerNetwork == null) return;
        if (FindTarget(out PlayerNetwork target))
        {
            // NetworkObject.ObjectId is uint
            int targetId = target.NetworkObject.ObjectId;
            DealDamageServerRpc(targetId, _damage);
        }
    }

    private bool FindTarget(out PlayerNetwork target)
    {
        target = null;
        if (_playerCamera == null) _playerCamera = Camera.main;
        if (_playerCamera == null) return false;

        Ray ray = _playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, _attackRange, _targetLayer))
        {
            target = hit.collider.GetComponentInParent<PlayerNetwork>();
            return target != null && target.IsSpawned;
        }
        return false;
    }

    [ServerRpc]
    private void DealDamageServerRpc(int targetObjectId, int damage)
    {
        if (!base.IsServerStarted) return;
        if (!InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(targetObjectId, out var targetObj))
        {
            Debug.LogWarning($"[Server] Target {targetObjectId} not found!");
            return;
        }
        PlayerNetwork targetPlayer = targetObj.GetComponent<PlayerNetwork>();
        if (targetPlayer == null || targetPlayer == _playerNetwork) return;

        float distance = Vector3.Distance(transform.position, targetObj.transform.position);
        if (distance > _attackRange)
        {
            Debug.LogWarning($"[Server] Target too far! Distance: {distance:F2}");
            return;
        }
        targetPlayer.HP.Value = Mathf.Max(0, targetPlayer.HP.Value - damage);
    }
}