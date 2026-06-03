using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CombatAreaManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject warningPanel;
    public TextMeshProUGUI countdownText;

    [Header("Settings")]
    public float countdownTime = 5f;

    private Coroutine countdownCoroutine;

    private void Start()
    {
        warningPanel.SetActive(false);
    }

    public void ShowWarning()
    {
        warningPanel.SetActive(true);

        if (countdownCoroutine == null)
        {
            countdownCoroutine = StartCoroutine(CountdownRoutine());
        }
    }

    public void HideWarning()
    {
        warningPanel.SetActive(false);

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        countdownText.text = countdownTime.ToString("0");
    }

    private IEnumerator CountdownRoutine()
    {
        float timer = countdownTime;

        while (timer > 0)
        {
            countdownText.text = Mathf.Ceil(timer).ToString();

            yield return null;

            timer -= Time.deltaTime;
        }

        countdownText.text = "0";

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}