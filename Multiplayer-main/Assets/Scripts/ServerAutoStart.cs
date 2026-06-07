using FishNet;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    private void Start()
    {
        // Если игра запущена с флагом -batchmode (без графики), стартуем сервер
        if (Application.isBatchMode)
        {
            Debug.Log("[Server] Headless mode detected. Starting server...");
            InstanceFinder.ServerManager.StartConnection();
        }
    }
}