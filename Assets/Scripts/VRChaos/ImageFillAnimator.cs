using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ImageFillAnimator : MonoBehaviour
{
    public List<Image> images = new List<Image>();
    public float fillDuration = 0.25f;

    private Image currentSelectedButtonImage;

    public void StartFilling(string contextName)
    {
        for (int i = 0; i < images.Count; i++)
        {
            if (images[i].gameObject.name == contextName)
            {
                images[i].gameObject.SetActive(true);
                currentSelectedButtonImage = images[i];
            }
            else
            {
                images[i].fillAmount = 0.0f;
                images[i].gameObject.SetActive(false);
            }
        }

        StartCoroutine(FillImage());
    }

    private IEnumerator FillImage()
    {
        currentSelectedButtonImage.fillAmount = 0.0f;
        float elapsed = 0.0f;

        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;

            currentSelectedButtonImage.fillAmount = Mathf.Clamp01(elapsed / fillDuration);

            yield return null;
        }

        currentSelectedButtonImage.fillAmount = 1.0f;
    }
}
