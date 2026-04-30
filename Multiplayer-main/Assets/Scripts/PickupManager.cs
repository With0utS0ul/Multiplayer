using FishNet.Managing;
using FishNet.Object;
using UnityEngine;
using System.Collections;
using FishNet;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private void Start()
    {
        if (!InstanceFinder.IsServerStarted) return;
        SpawnAll();
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
        GameObject go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);
        go.GetComponent<HealthPickup>().Init(this);
        InstanceFinder.ServerManager.Spawn(go);
    }
}