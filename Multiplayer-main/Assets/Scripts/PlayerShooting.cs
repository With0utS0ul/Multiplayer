using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;
    [SerializeField] public int _maxAmmo = 10;

    private float _lastShotTime;
    public readonly SyncVar<int> CurrentAmmo = new SyncVar<int>();
    public event System.Action<int> OnAmmoChangedUI;

    private PlayerNetwork _playerNetwork;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _playerNetwork = GetComponent<PlayerNetwork>();
        if (base.IsServerStarted)
            CurrentAmmo.Value = _maxAmmo;
        CurrentAmmo.OnChange += OnAmmoChanged;
        OnAmmoChanged(CurrentAmmo.Value, CurrentAmmo.Value, false);
    }

    public override void OnStopNetwork()
    {
        CurrentAmmo.OnChange -= OnAmmoChanged;
    }

    private void OnAmmoChanged(int oldVal, int newVal, bool asServer)
    {
        OnAmmoChangedUI?.Invoke(newVal);
    }

    private void Update()
    {
        if (!base.Owner.IsLocalClient) return;
        if (Input.GetKeyDown(KeyCode.Space))
            ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir, NetworkConnection conn = null)
    {
        if (_playerNetwork.HP.Value <= 0) return;
        if (CurrentAmmo.Value <= 0) return;
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        CurrentAmmo.Value--;

        GameObject go = Instantiate(_projectilePrefab, pos + dir * 1.2f, Quaternion.LookRotation(dir));
        base.ServerManager.Spawn(go, conn);
    }
}