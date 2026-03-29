using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;
using System.Linq;

/// <summary>
/// PlayerUnlockedCharacter0
/// PlayerHoldCharacter0InStory
/// StoryModeHasQuest0
/// StoryModeHoldCard0
/// </summary>
public enum GameMode{ story, free}
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    public GameMode gameMode;
    public static bool GameStarted = false;
    public static bool BattleStarted = false;
    private static int BattleScene = 1;
    
    [Header("Systems")]
    public GameObject GameCharacters;
    public GameObject GameEventUI;
    
    public ToolController toolController;
    
    public static Character pendingInitialCharacter;

    [Header("Fade")]
    public CanvasGroup canvasGroup;
    public TMP_Text tip;
    [SerializeField] private float fadeDuration = 1f;
    [Header("Data")]
    public List<string> fadeTips = new List<string>();
    public List<Card> defaultCards = new List<Card>();
    public List<Card> cardDatas = new List<Card>();
    public List<Character> characterDatas = new List<Character>();
    public List<QuestData> questDatas = new List<QuestData>();
    public List<StoryEnd> StoryEndDatas = new List<StoryEnd>();

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
        yield return (LoadScene(BattleScene));
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
        GamecharacterControl.CanMove = true;
        foreach(var character in characterDatas)
        {
            PlayerPrefs.SetInt($"PlayerHoldCharacter{GameModeManager.Instance.characterDatas.IndexOf(character)}InStory", 0);
        }
        StartCoroutine(StoryModeBegin());
    }
    private IEnumerator StoryModeBegin()
    {
        yield return (LoadScene(BattleScene));
        yield return null;
        yield return null;
        
        GameCharacters?.SetActive(true);
        GameEventUI?.SetActive(true);
        EventUI.Instance?.StoryUI.SetActive(true);
        EventUI.Instance?.SetPanel.SetActive(false);
        EventUI.Instance?.eventPanel.SetActive(false);
        EventUI.Instance?.DialoguePanel.SetActive(false);
        EventUI.Instance?.panel.SetActive(false);

        QuestManager.Instance?.SetStoryMode();

        StoryModeManager.cards.AddRange(defaultCards);
        StoryModeManager.Instance?.RemakeLevel();

        if (pendingInitialCharacter != null)
        {
            StoryModeManager.Instance?.GetNewMenber(pendingInitialCharacter);
        }

        GameCharacterManager.Instance.SetActing(GameCharacterManager.Instance.actingCharacter);
        GameCharacterManager.Instance.StorySet();
    }

    public void ContinueStory() // 繼續遊戲
    {
        gameMode = GameMode.story;
        GameStarted = true;
        GamecharacterControl.CanMove = true;
        StartCoroutine(ContinueStoryBegin());
    }
    private IEnumerator ContinueStoryBegin()
    {
        // 加入事件
        yield return (LoadScene(BattleScene));
        yield return null;
        yield return null;

        GameCharacters?.SetActive(true);
        GameEventUI?.SetActive(true);
        QuestManager.Instance?.SetStoryMode();

        foreach(var character in characterDatas)
        {
            if (PlayerPrefs.GetInt($"PlayerHoldCharacter{characterDatas.IndexOf(character)}InStory")== 1)
                StoryModeManager.Instance?.GetNewMenber(character);
        }
        foreach(var quest in questDatas)
        {
            if (PlayerPrefs.GetInt($"StoryModeHasQuest{questDatas.IndexOf(quest)}") == -1) continue;

            QuestManager.Instance?.SetQuest(quest, PlayerPrefs.GetInt($"StoryModeHasQuest{questDatas.IndexOf(quest)}"));
        }
        foreach(var card in cardDatas)
        {
            if (PlayerPrefs.GetInt($"StoryModeHoldCard{GameModeManager.Instance.cardDatas.IndexOf(card)}") == 1)
                StoryModeManager.cards.Add(card);
        }
        
        GameCharacterManager.Instance.SetActing(GameCharacterManager.Instance.actingCharacter);
        GameCharacterManager.Instance.StorySet();
    }

    public void StartStoryBattle(EventBattleData data)
    {
        Debug.Log("開始故事戰鬥");
        BattleStarted = true;
        
        StartCoroutine(StoryBattleBegin(data));
    }
    private IEnumerator StoryBattleBegin(EventBattleData data)
    {
        yield return (LoadScene(BattleScene));
        yield return null;
        GameCharacters.SetActive(false);
        EventUI.Instance?.BattleBegin(data);
        ToolController.isPause = false;

        yield return new WaitUntil(()=> TurnManager.Instance != null && CharacterSelectionManager.Instance != null);
        CharacterSelectionManager.Instance.SetStoryCharacter(data);
    }

    #endregion
    
    #region Loading
    // 轉場
    private IEnumerator LoadScene(int index)
    {
        if (Time.timeScale != 1)
        {
            Time.timeScale = 1;
        }

        if (fadeTips.Count > 0)
            tip.text = fadeTips[Random.Range(0, fadeTips.Count)];
        yield return FadeIn();

        // 等場景完全啟用
        AsyncOperation async = SceneManager.LoadSceneAsync(index, LoadSceneMode.Single);
        while (!async.isDone)
            yield return null;

        yield return new WaitForSeconds(0.3f);
        MenuManager.Instance.CheckGameMode();
        EventUI.NowScene = index;

        if (gameMode == GameMode.story && GameStarted)
        {
            GameCharacterManager.Instance.StorySet();
        }

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
    #endregion

    #region Over
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
            yield return (LoadScene(BattleScene));
            yield return null;

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

    public IEnumerator StoryModeEnd()
    {
        PlayerPrefs.SetInt("StroyBegin", 0);
        GamecharacterControl.CanMove = false;
        foreach(var character in characterDatas)
        {
            PlayerPrefs.SetInt($"PlayerHoldCharacter{GameModeManager.Instance.characterDatas.IndexOf(character)}InStory", 0);
        }
        foreach(var card in cardDatas)
        {
            PlayerPrefs.SetInt($"StoryModeHoldCard{GameModeManager.Instance.cardDatas.IndexOf(card)}", 0);
        }
        foreach(var quest in QuestManager.MainQuests)
        {
            PlayerPrefs.SetInt($"StoryModeHasQuest{questDatas.IndexOf(quest)}", -1);
        }
        StoryModeManager.cards.Clear();
        
        yield return BackToMenu();
    }
    public IEnumerator BackToMenu()
    {
        GameStarted = false;
        BattleStarted = false;
        GameCharacterManager.Instance.actingCharacter = null;
        yield return (LoadScene(BattleScene));
        yield return null;

        if(MenuManager.Instance != null)
        {
            MenuManager.Instance.battleCanvas.SetActive(false);
            MenuManager.Instance.gameObject.SetActive(true);
        }

        GameCharacters.SetActive(false);
        GameEventUI.SetActive(false);
        EventUI.InEvent = false;
    }

    #endregion
}
