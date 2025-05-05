using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private Canvas fadeCanvas;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeSpeed = 1.5f;

    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image HUDImage;

    private Canvas faderCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!fadeCanvas.enabled)
        {
            fadeCanvas.enabled = true;
        }
    }

    public void InitializeForLocalPlayer(Camera playerCamera)
    {
        if (Instance != null)
        {
            if (fadeCanvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                fadeCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            }

            fadeCanvas.worldCamera = playerCamera;

            if (!fadeCanvas.enabled)
            {
                fadeCanvas.enabled = true;
            }

            FadeFromBlack();
        }
    }

    public void ShowMessage(string message, float duration = 2.0f)
    {
        if (Instance != null)
        {
            StartCoroutine(ShowMessageRoutine(message, duration));
        }
    }

    public void SetCountdown(float timeRemaining)
    {
        if (Instance != null)
        {
            countdownText.text = Mathf.CeilToInt(timeRemaining).ToString();

            if (!countdownText.gameObject.activeSelf)
            {
                countdownText.gameObject.SetActive(true);
            }
        }
    }

    public void SetFinalCountdown(float timeRemaining)
    {
        if (Instance != null)
        {
            StartCoroutine(StartFinalCountdown((int)timeRemaining));
        }
    }

    public void HideCountdown()
    {
        if (Instance != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    public void ShowHUDImage(Sprite image, float duration = 2.0f)
    {
        if (Instance != null)
        {
            StartCoroutine(ShowHUDImageRoutine(image, duration));
        }
    }

    public void FadeToBlack()
    {
        if (Instance != null)
        {
            StartCoroutine(Fade(1.0f));
        }
    }

    public void FadeFromBlack()
    {
        if (Instance != null)
        {
            StartCoroutine(Fade(0.0f));
        }
    }

    public void FadeAndThen(System.Action afterFade)
    {
        if (Instance != null)
        {
            StartCoroutine(FadeThen(1.0f, afterFade));
        }
    }

    public void ResetHUD()
    {
        if (Instance != null)
        {
            fadeCanvas.worldCamera = Camera.main;
            FadeFromBlack();
        }
    }

    public IEnumerator ShowMessageRoutine(string message, float duration)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        messageText.alpha = 1.0f;

        yield return new WaitForSeconds(duration);

        float t = 0.0f;
        
        while (t < 1.0f)
        {
            t += Time.deltaTime;
            messageText.alpha = Mathf.Lerp(1.0f, 0.0f, t);
            yield return null;
        }

        messageText.gameObject.SetActive(false);
    }

    public IEnumerator StartFinalCountdown(int from)
    {
        for (int i = from; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            countdownText.transform.localScale = Vector3.one * 0.5f;
            countdownText.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            if (!countdownText.gameObject.activeSelf)
            {
                countdownText.gameObject.SetActive(true);
            }

            float elapsed = 0.0f;
            float duration = 1.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(0.5f, 1.2f, elapsed / duration);
                float alpha = Mathf.Lerp(1.0f, 0.0f, elapsed / duration);

                countdownText.transform.localScale = Vector3.one * scale;
                countdownText.color = new Color(1.0f, 1.0f, 1.0f, alpha);

                yield return null;
            }

            countdownText.gameObject.SetActive(false);
        }
    }

    public IEnumerator ShowHUDImageRoutine(Sprite image, float duration)
    {
        HUDImage.sprite = image;
        HUDImage.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        HUDImage.gameObject.SetActive(false);
    }

    public IEnumerator Fade(float targetAlpha)
    {
        fadeCanvasGroup.gameObject.SetActive(true);

        while (!Mathf.Approximately(fadeCanvasGroup.alpha, targetAlpha))
        {
            fadeCanvasGroup.alpha = Mathf.MoveTowards(fadeCanvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            yield return null;
        }
    }

    public IEnumerator FadeThen(float targetAlpha, System.Action afterFade)
    {
        yield return Fade(targetAlpha);

        afterFade?.Invoke();
    }
}
