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

    private bool isDone = false;

    string[] facts = new string[]
    {
        "The 81-day battle at Quang Tri Citadel became a symbol of resilience.",
        "Many soldiers fighting in Quang Tri were young students and volunteers.",
        "Every inch of Quang Tri Citadel witnessed immense sacrifice.",
        "The intensity of bombing in Quang Tri was among the heaviest in the war.",
        "Quang Tri Citadel stands as a symbol of courage and endurance.",
        "The Thach Han River was crossed by countless soldiers during the battle.",
        "Young soldiers played a crucial role in the 81-day defense.",
        "Many fighters went to war at a very young age.",
        "The sacrifices at Quang Tri helped shape the course of the war.",
        "Quang Tri Citadel represents the bravery of an entire generation."
    };

    void Start()
    {
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
            if (Input.GetMouseButtonDown(0))
            {
                SceneManager.LoadScene("EmptyScene");
            }
        }
    }

    IEnumerator LoadScene()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync("EmptyScene");
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            slider.value = progress;
            progressText.text = (progress * 100f).ToString("F0") + "%";

            if (progress >= 1f)
            {
                isDone = true;
                continueText.gameObject.SetActive(true);

                if (Input.GetMouseButtonDown(0))
                {
                    operation.allowSceneActivation = true;
                }
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

            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator AnimateLoadingText()
    {
        string baseText = "Entering the operation";
        int dotCount = 0;

        while (true)
        {
            dotCount = (dotCount % 3) + 1;

            loadingText.text = baseText + new string('.', dotCount);

            yield return new WaitForSeconds(0.5f);
        }
    }
}