using FishNet.Managing;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class NetworkServerStarter : MonoBehaviour
{
    private void Start()
    {
#if UNITY_SERVER
        Debug.Log("=== SERVER BUILD: Attempting to start server ===");
        NetworkManager manager = GetComponent<NetworkManager>();
        if (manager != null)
        {
            Debug.Log("NetworkManager found. Starting server...");
            manager.ServerManager.StartConnection();
            Debug.Log("StartConnection called. Server should start.");
        }
        else
        {
            Debug.LogError("NetworkManager not found!");
        }
#endif
    }
}