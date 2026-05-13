using FishNet;
using FishNet.Managing;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    [SerializeField] private ushort port = 7770;

    private void Start()
    {
        if (Application.isBatchMode)
        {
            Debug.Log($"[Server] Starting on port {port}");
            InstanceFinder.ServerManager.StartConnection(port);
        }
    }
}