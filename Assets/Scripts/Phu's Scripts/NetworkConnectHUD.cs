using UnityEngine;
using PurrNet;
using PurrNet.Transports;

namespace PhuScene
{
    public class NetworkConnectHUD : MonoBehaviour
    {
        [SerializeField] private int guiOffsetX = 10;
        [SerializeField] private int guiOffsetY = 10;

        private void OnGUI()
        {
            var nm = NetworkManager.main;
            if (!nm)
            {
                GUILayout.BeginArea(new Rect(guiOffsetX, guiOffsetY, 300, 100));
                GUILayout.Label("No NetworkManager.main found in scene.");
                GUILayout.EndArea();
                return;
            }

            GUILayout.BeginArea(new Rect(guiOffsetX, guiOffsetY, 250, 200), "Network HUD", GUI.skin.box);
            
            GUILayout.Space(15);
            GUILayout.Label($"Server: {nm.serverState}");
            GUILayout.Label($"Client: {nm.clientState}");
            GUILayout.Space(5);

            if (nm.serverState == ConnectionState.Disconnected && nm.clientState == ConnectionState.Disconnected)
            {
                if (GUILayout.Button("Host (Server + Client)"))
                {
                    nm.StartServer();
                    nm.StartClient();
                }

                if (GUILayout.Button("Start Server"))
                {
                    nm.StartServer();
                }

                if (GUILayout.Button("Start Client"))
                {
                    nm.StartClient();
                }
            }
            else
            {
                if (nm.serverState != ConnectionState.Disconnected)
                {
                    if (GUILayout.Button("Stop Server"))
                    {
                        nm.StopServer();
                    }
                }

                if (nm.clientState != ConnectionState.Disconnected)
                {
                    if (GUILayout.Button("Stop Client"))
                    {
                        nm.StopClient();
                    }
                }
            }

            GUILayout.EndArea();
        }
    }
}
