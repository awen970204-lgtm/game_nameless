using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class CardToggle : MonoBehaviour
{
    public Toggle toggle;
    public Image cardPicture;
    public Text cardName;
    public Card card; // 對應卡牌資料
    public int player;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleChange);
    }
    void Start()
    {
        cardPicture.sprite = card.cardPicture;
        cardName.text = card.cardName;
        if (player == 1 && 
            (!MenuManager.player1Cards.Contains(card) || 
            PlayerPrefs.GetInt($"FreeModeCard_Player1Hold{GameModeManager.Instance.cardDatas.IndexOf(card)}") == 0))
        {
            toggle.SetIsOnWithoutNotify(false);
        }
        if (player == 2 && 
            (!MenuManager.player2Cards.Contains(card) || 
            PlayerPrefs.GetInt($"FreeModeCard_Player1Hold{GameModeManager.Instance.cardDatas.IndexOf(card)}") == 0))
        {
            toggle.SetIsOnWithoutNotify(false);
        }
    }

    void OnToggleChange(bool isOn)
    {
        MenuManager.Instance?.SetCardDeck(card, player, isOn);
    }
}
