using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingController : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI continueText;
    public TextMeshProUGUI factText;
    public TextMeshProUGUI loadingText;
    [Tooltip("Enter the exact name of the scene to load")]
    [SerializeField] private string sceneToLoad;
    [Header("Fact Settings")]
    [Tooltip("How many seconds the fact stays on screen before fading out")]
    [SerializeField] private float factDisplayDuration = 4.5f;
    private AsyncOperation operation;
    private bool isDone = false;

    string[] facts = new string[]
    {
        "The firepower poured into Quang Tri Citadel equaled the destructive force of seven Hiroshima atomic bombs.",
        "Many defenders were university students from Hanoi who left their studies to join the fight in 1971.",
        "The Thach Han River was the only supply line. Crossing it at night under enemy flare light was a deadly mission.",
        "Quang Tri Citadel stands as a symbol of courage and endurance.",
        "Despite being only 500x500 meters in size, the Citadel became the bloodiest focal point of the 1972 offensive."
    };

    void Start()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("[LoadingController] Scene to load is missing! Please type it into the Inspector.");
            return;
        }
        continueText.gameObject.SetActive(false);

        StartCoroutine(LoadScene());
        StartCoroutine(ChangeFact());
        StartCoroutine(AnimateLoadingText()); // 🔥 thêm animation text
    }

    void Update()
    {
        if (isDone)
        {
            // nhấp nháy "Click to continue"
            float alpha = Mathf.PingPong(Time.time * 1.5f, 1f);
            continueText.alpha = alpha;

            // click để qua scene
            if (Input.GetMouseButtonDown(0) && operation != null)
            {
                operation.allowSceneActivation = true;
            }
        }
    }

    IEnumerator LoadScene()
    {
        operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            slider.value = progress;
            progressText.text = (progress * 100f).ToString("F0") + "%";

            if (operation.progress >= 0.9f)
            {
                isDone = true;
                continueText.gameObject.SetActive(true);
            }

            yield return null;
        }
    }

    IEnumerator ChangeFact()
    {
        while (true)
        {
            int randomIndex = Random.Range(0, facts.Length);

            // fade out
            for (float t = 1; t > 0; t -= Time.deltaTime)
            {
                factText.alpha = t;
                yield return null;
            }

            // đổi fact (có màu + in nghiêng)
            factText.text = "<b><color=#D4AF37>Did you know?</color></b> <i>" + facts[randomIndex] + "</i>";

            // fade in
            for (float t = 0; t < 1; t += Time.deltaTime)
            {
                factText.alpha = t;
                yield return null;
            }

            yield return new WaitForSeconds(factDisplayDuration);
        }
    }

  IEnumerator AnimateLoadingText()
{
    string baseText = "Entering the battlefield";
    int dotCount = 0;

    while (true)
    {
        dotCount = (dotCount % 3) + 1;
        loadingText.text = baseText + new string('.', dotCount);
        yield return new WaitForSeconds(0.5f);
    }
}
}