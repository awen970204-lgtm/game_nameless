using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputValue : MonoBehaviour
{
    public TMP_Text numberText;
    private int value;
    private int maxValue;

    void Awake()
    {
        value = 1;
        UpdateUI();
    }

    public void CanInput(int max)
    {
        gameObject.SetActive(true);
        maxValue = max;
    }

    public void CloseInput()
    {
        value = 1;
        maxValue = 1;
        UpdateUI();
        gameObject.SetActive(false);
    }
    public void TryToIncrease()// 增加
    {
        if (TurnManager.Instance.pendingUser.team != TeamID.Enemy)
            IncreaseValue();
    }
    public void IncreaseValue()
    {
        if (value < maxValue) value++;
        UpdateUI();
        Debug.Log("輸入值+1");
    }
    public void TryToDecrease()// 減少
    {
        if (TurnManager.Instance.pendingUser.team != TeamID.Enemy)
            DecreaseValue();
    }
    public void DecreaseValue()
    {
        if (value > 0) value--;
        UpdateUI();
        Debug.Log("輸入值-1");
    }

    public void ChangeValueToFixed(int number)
    {
        value = number;
        UpdateUI();
    }

    private void UpdateUI()
    {
        numberText.text = $"{value}";
    }

    public void ChangeEnterValue()
    {
        if (TurnManager.Instance.pendingUser != null)
            TurnManager.Instance.pendingUser.EnterValue = value;
    }
}
