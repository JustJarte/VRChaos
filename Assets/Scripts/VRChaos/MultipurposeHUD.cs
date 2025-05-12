using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Acts as the instance of the in-game HUD. Only applies itself to the local player, so it isn't visible from another player's point of view. Allows in game messages and graphics to be seen, and additionally also handles
// fading functionality to hide scene changing for the player.
public class MultipurposeHUD : MonoBehaviour
{
    public static MultipurposeHUD Instance { get; private set; }

    [SerializeField] private Canvas fadeCanvas;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeSpeed = 1.5f;

    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image HUDImage;

    private Canvas faderCanvas;

    // Create instance.
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

    // When called, it sets RenderMode to ScreenSpaceCamera if it's not already, and then sets its reference camera to the local player's main camera, then fades back from black.
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

    // Starts a Coroutine that displays a given message for the given amount of time, otherwise default is 2 seconds.
    public void ShowMessage(string message, float duration = 2.0f)
    {
        if (Instance != null)
        {
            StartCoroutine(ShowMessageRoutine(message, duration));
        }
    }

    // Displays a countdown number.
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

    // Starts Coroutine that begins a final countdown wherein emphasis is placed on the countdown of the remaining amount of time passed in, generally started at 10 seconds.
    public void SetFinalCountdown(float timeRemaining)
    {
        if (Instance != null)
        {
            StartCoroutine(StartFinalCountdown((int)timeRemaining));
        }
    }

    // Hides the countdownText object as needed by making it inactive.
    public void HideCountdown()
    {
        if (Instance != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    // Starts a Coroutine that displays a given Image for the given amount of time, otherwise default is 2 seconds.
    public void ShowHUDImage(Sprite image, float duration = 2.0f)
    {
        if (Instance != null)
        {
            StartCoroutine(ShowHUDImageRoutine(image, duration));
        }
    }

    // Starts a Coroutine that performs a fade effect to obscure player vision.
    public void FadeToBlack()
    {
        if (Instance != null)
        {
            StartCoroutine(Fade(1.0f));
        }
    }

    // Calls the same Coroutine as above, but inverses the alpha amount so that the fade is to return player vision.
    public void FadeFromBlack()
    {
        if (Instance != null)
        {
            StartCoroutine(Fade(0.0f));
        }
    }

    // Starts a Coroutine that performs a fade effect to obscure player vision, then additionally performs an Action afterwards as needed.
    public void FadeAndThen(System.Action afterFade)
    {
        if (Instance != null)
        {
            StartCoroutine(FadeThen(1.0f, afterFade));
        }
    }

    // Quick reset of the HUD, not generally called.
    public void ResetHUD()
    {
        if (Instance != null)
        {
            fadeCanvas.worldCamera = Camera.main;
            FadeFromBlack();
        }
    }

    // Coroutine that shows message for a given duration, with a slight fade effect as the duration passes.
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

    // Coroutine that shows a final countdown starting at a specified number. Each number grows larger and fades away to emphasize the final countdown before a match begins/ends.
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

    // Coroutine that shows a given Image for a given duration then sets it inactive.
    public IEnumerator ShowHUDImageRoutine(Sprite image, float duration)
    {
        HUDImage.sprite = image;
        HUDImage.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        HUDImage.gameObject.SetActive(false);
    }

    // Coroutine that handles the fade effect by changing the canvas group's alpha over time.
    public IEnumerator Fade(float targetAlpha)
    {
        fadeCanvasGroup.gameObject.SetActive(true);

        while (!Mathf.Approximately(fadeCanvasGroup.alpha, targetAlpha))
        {
            fadeCanvasGroup.alpha = Mathf.MoveTowards(fadeCanvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            yield return null;
        }
    }

    // Coroutine that handles the fade ffect by changing the canvas group's alpha over time, and then invokes a given Action.
    public IEnumerator FadeThen(float targetAlpha, System.Action afterFade)
    {
        yield return Fade(targetAlpha);

        afterFade?.Invoke();
    }
}
