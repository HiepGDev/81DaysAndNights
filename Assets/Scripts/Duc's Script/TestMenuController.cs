using UnityEngine;
using UnityEngine.SceneManagement;

public class TestMenuController : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("LoadingScene");
    }
}