// using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    public Image background;
    public Image progressBar;
    public TMP_Text progressText;
    private Coroutine loading = null;

    public void StartLoadingGame()
    {
        if (loading == null)
        {
            background.transform.GetChild(0).gameObject.SetActive(true);
            background.transform.GetChild(1).gameObject.SetActive(false);
            loading = StartCoroutine(LoadingGame());
        }
    }

    private IEnumerator LoadingGame()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(1);
        operation.allowSceneActivation = false;
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            progressBar.fillAmount = progress;
            progressText.text = $"{progress * 100}%";

            if (operation.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.5f);
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
