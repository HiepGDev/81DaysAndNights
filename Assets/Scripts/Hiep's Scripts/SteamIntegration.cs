using UnityEngine;
using Steamworks;

public class SteamIntegration : MonoBehaviour
{
    public static SteamIntegration Instance;

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

    private void Start()
    {
        try
        {
            // Init with App ID. true = catch errors
            SteamClient.Init(5042430, true);
            Debug.Log("Steam Connected! Welcome " + SteamClient.Name);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Steam failed to connect: " + e.Message);
        }
    }

    private void Update()
    {
        // Facepunch needs this to handle callbacks (like unlocking achievements)
        SteamClient.RunCallbacks();
    }


    private void OnApplicationQuit()
    {
        SteamClient.Shutdown();
    }

}
