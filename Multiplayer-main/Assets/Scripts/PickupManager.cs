using FishNet;
using FishNet.Managing;
using FishNet.Object;
using System.Collections;
using UnityEngine;
using FishNet.Transporting;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private NetworkManager _networkManager;

    private void Start()
    {
        _networkManager = InstanceFinder.NetworkManager;
        if (_networkManager == null)
        {
            Debug.LogError("PickupManager: NetworkManager not found!");
            return;
        }

        // Подписываемся на событие изменения состояния сервера
        _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;

        // Если сервер уже запущен до подписки, вызываем спавн немедленно
        if (_networkManager.ServerManager.Started)
        {
            SpawnAll();
        }
    }

    private void OnDestroy()
    {
        // Важно: отписываемся от события при уничтожении объекта
        if (_networkManager != null && _networkManager.ServerManager != null)
        {
            _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        }
    }

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        // Проверяем, что состояние изменилось на 'Запущен'
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            Debug.Log("PickupManager: Server started, spawning pickups...");
            SpawnAll();
        }
    }

    private void SpawnAll()
    {
        foreach (var point in _spawnPoints)
            SpawnPickup(point.position);
    }

    public void OnPickedUp(Vector3 position)
    {
        StartCoroutine(RespawnAfterDelay(position));
    }

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnPickup(position);
    }

    private void SpawnPickup(Vector3 position)
    {
        if (!InstanceFinder.IsServerStarted) return;
        GameObject go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);
        var pickup = go.GetComponent<HealthPickup>();
        if (pickup != null) pickup.Init(this);
        InstanceFinder.ServerManager.Spawn(go);
    }
}