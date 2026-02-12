using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

public enum GameMode{ story, free}
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    public GameMode gameMode;
    public static bool GameStarted = false;
    public static bool BattleStarted = false;
    
    [Header("Systems")]
    public GameObject GameCharacters;
    public GameObject GameEventUI;
    
    public ToolController toolController;
    
    public static Character pendingInitialCharacter;

    [Header("Fade")]
    public CanvasGroup canvasGroup;
    public float fadeDuration = 1f;
    [Header("Data")]
    public List<Card> cardDatas = new List<Card>();
    public List<Character> characterDatas = new List<Character>();
    public List<QuestData> questDatas = new List<QuestData>();

    void Awake()
    {
        if (Instance == null) 
            Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        DontDestroyOnLoad(GameCharacters);
        DontDestroyOnLoad(GameEventUI);
        DontDestroyOnLoad(this.gameObject);
    }

    #region FreeMode

    public void StartFreeMode()
    {
        gameMode = GameMode.free;
        GameStarted = true;
        BattleStarted = true;
        StartCoroutine(FreeModeBegin());
    }
    private IEnumerator FreeModeBegin()
    {
        yield return (LoadScene(0));
        yield return null;
        GameCharacters?.SetActive(false);
        GameEventUI?.SetActive(false);
    }

    #endregion

    #region StoryMode
    // 劇情模式
    public void StartStoryMode()
    {
        if (PlayerPrefs.GetInt($"PlayerUnlockedCharacter{characterDatas.IndexOf(pendingInitialCharacter)}") == 0)
            return;

        gameMode = GameMode.story;
        GameStarted = true;
        StartCoroutine(StoryModeBegin());
    }
    private IEnumerator StoryModeBegin()
    {
        yield return (LoadScene(0));
        yield return null;
        GameCharacters?.SetActive(true);
        GameEventUI?.SetActive(true);
        EventUI.Instance?.StoryUI.SetActive(true);
        EventUI.Instance?.SetPanel.SetActive(false);
        EventUI.Instance?.eventPanel.SetActive(false);
        EventUI.Instance?.DialoguePanel.SetActive(false);
        EventUI.Instance?.panel.SetActive(false);

        QuestManager.Instance?.SetStoryMode();

        if (pendingInitialCharacter != null)
        {
            StoryModeManager.Instance?.GetNewMenber(pendingInitialCharacter);
            if (pendingInitialCharacter != characterDatas[0])
            {
                StoryModeManager.Instance?.GetNewMenber(characterDatas[0]);
            }
        }

        GameCharacterManager.Instance.SetActing(GameCharacterManager.Instance.actingCharacter);
    }

    public void StartStoryBattle(EventBattleData data)
    {
        Debug.Log("開始故事戰鬥");
        BattleStarted = true;
        
        StartCoroutine(StoryBattleBegin(data));
    }
    private IEnumerator StoryBattleBegin(EventBattleData data)
    {
        yield return (LoadScene(0));
        yield return null;
        GameCharacters.SetActive(false);
        EventUI.Instance?.BattleBegin(data);
        ToolController.isPause = false;

        yield return new WaitUntil(()=> TurnManager.Instance != null && CharacterSelectionManager.Instance != null);
        CharacterSelectionManager.Instance.SetStoryCharacter(data);
    }

    #endregion
    
    // 轉場
    private IEnumerator LoadScene(int index)
    {
        if (Time.timeScale != 1)
        {
            Time.timeScale = 1;
        }

        yield return FadeIn();

        // 等場景完全啟用
        AsyncOperation async = SceneManager.LoadSceneAsync(index, LoadSceneMode.Single);
        while (!async.isDone)
            yield return null;

        yield return new WaitForSeconds(0.3f);
        MenuManager.Instance.CheckGameMode();
        StartCoroutine(FadeOut());
    }
    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
    private IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    // 遊戲結束
    public void BattleOver()
    {
        StartCoroutine(BattleOverCheck());
    }
    private IEnumerator BattleOverCheck()
    {
        Debug.Log("戰鬥結束");
        if (gameMode == GameMode.free)
        {
            GameStarted = false;
            BattleStarted = false;
            yield return (LoadScene(0));

            if(MenuManager.Instance != null)
            {
                MenuManager.Instance.battleCanvas.SetActive(false);
                MenuManager.Instance.gameObject.SetActive(true);
            }

            GameCharacters.SetActive(false);
            GameEventUI.SetActive(false);

        }
        else if (gameMode == GameMode.story)
        {
            BattleStarted = false;
            yield return LoadScene(EventUI.NowScene);
            yield return null;

            GameCharacters.SetActive(true);
            GameEventUI.SetActive(true);
        }

        yield return null;
        
        EventUI.Instance?.BattleOverCheck();
    }

    public IEnumerator BackToMenu()
    {
        GameStarted = false;
        BattleStarted = false;
        GameCharacterManager.Instance.actingCharacter = null;
        yield return (LoadScene(0));
        yield return null;

        if(MenuManager.Instance != null)
        {
            MenuManager.Instance.battleCanvas.SetActive(false);
            MenuManager.Instance.gameObject.SetActive(true);
        }

        GameCharacters.SetActive(false);
        GameEventUI.SetActive(false);
    }
}
