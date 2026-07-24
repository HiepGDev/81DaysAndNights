using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Localization;

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
    [Header("Localization Setup")]
    [SerializeField] private LocalizedString[] localizedFacts;
    [SerializeField] private LocalizedString localizedDidYouKnow;
    [SerializeField] private LocalizedString localizedEnteringBattlefield;
    private AsyncOperation operation;
    private bool isDone = false;

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
            if (localizedFacts == null || localizedFacts.Length == 0)
            {
                yield break;
            }
            int randomIndex = Random.Range(0, localizedFacts.Length);

            // fade out
            for (float t = 1; t > 0; t -= Time.deltaTime)
            {
                factText.alpha = t;
                yield return null;
            }
            string didYouKnowStr = localizedDidYouKnow.GetLocalizedString();
            string factStr = localizedFacts[randomIndex].GetLocalizedString();

            // đổi fact (có màu + in nghiêng)
            factText.text = $"<b><color=#D4AF37>{didYouKnowStr}</color></b> <i>{factStr}</i>";

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
    int dotCount = 0;

    while (true)
    {
        string baseText = localizedEnteringBattlefield.GetLocalizedString();
        
        dotCount = (dotCount % 3) + 1;
        loadingText.text = baseText + new string('.', dotCount);
        yield return new WaitForSeconds(0.5f);
    }
}
}