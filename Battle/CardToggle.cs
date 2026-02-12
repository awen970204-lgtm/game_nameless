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
        if (player == 1 && !MenuManager.player1Cards.Contains(card))
            toggle.isOn = false;
        if (player == 2 && !MenuManager.player2Cards.Contains(card))
            toggle.isOn = false;
    }

    void OnToggleChange(bool isOn)
    {
        MenuManager.Instance?.SetCardDeck(card, player, isOn);
    }
}
