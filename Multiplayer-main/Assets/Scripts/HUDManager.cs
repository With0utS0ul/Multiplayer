using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text NicknameText;
    public TMP_Text HPText;
    public TMP_Text AmmoText;
    public TMP_Text RespawnTimerText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}