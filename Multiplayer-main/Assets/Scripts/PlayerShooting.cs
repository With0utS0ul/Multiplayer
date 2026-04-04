using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;
    [SerializeField] public int _maxAmmo = 10;

    private float _lastShotTime;
    private int _currentAmmo;


    public NetworkVariable<int> CurrentAmmo = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    // ������� ��� UI (���������� �� ���� �������� ��� ��������� ��������)
    public event Action<int> OnAmmoChanged;


    // IsAlive � ����� � PlayerNetwork ������������� ���������
    private PlayerNetwork _playerNetwork;

    public override void OnNetworkSpawn()
    {
        _currentAmmo = _maxAmmo;
        _playerNetwork = GetComponent<PlayerNetwork>();

        if (IsServer)
        {
            CurrentAmmo.Value = _maxAmmo;
        }

        // ������������� �� ��������� NetworkVariable
        CurrentAmmo.OnValueChanged += OnAmmoValueChanged;

        // ���� ��� ���� �������� - ����� ������� �������
        if (IsSpawned)
            OnAmmoValueChanged(default, CurrentAmmo.Value);
    }

    public override void OnNetworkDespawn()
    {
        CurrentAmmo.OnValueChanged -= OnAmmoValueChanged;
    }


    private void OnAmmoValueChanged(int previous, int current)
    {
        OnAmmoChanged?.Invoke(current);
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKeyDown(KeyCode.Space))
            ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir,
                                 ServerRpcParams rpc = default)
    {
        // 1. ��� �� �����?
        if (_playerNetwork.HP.Value <= 0) return;

        // 2. ���� �� �������?
        if (_currentAmmo <= 0) return;

        // 3. ������ �� �������?
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        _currentAmmo--;
        CurrentAmmo.Value--;

        var go = Instantiate(_projectilePrefab, pos + dir * 1.2f,
                             Quaternion.LookRotation(dir));
        var no = go.GetComponent<NetworkObject>();
        no.SpawnWithOwnership(rpc.Receive.SenderClientId);
    }

}