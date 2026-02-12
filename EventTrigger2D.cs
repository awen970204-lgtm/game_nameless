using UnityEngine;
using UnityEngine.InputSystem;

public class EventTrigger2D : MonoBehaviour
{
    public EventData eventData;

    public EventData openEventData;
    public EventData questEventData;

    private bool playerInside = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            EventUI.Instance.OnPlayerInEvent(eventData);
            playerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            EventUI.Instance.OnPlayerExitEvent();
            playerInside = false;
        }
    }
    
    void Start()
    {
        CheckEvent();
    }
    void Update()
    {
        if (playerInside && Keyboard.current.fKey.isPressed)
        {
            if (GameModeManager.GameStarted)
            {
                Debug.Log($"事件:{eventData.eventName}觸發");
                playerInside = false;
                EventUI.Instance.TriggerPendingEvent();
            }
        }
    }

    private void CheckEvent()
    {
        if (QuestManager.OverEvents.Contains(openEventData) || openEventData == null)
            gameObject.SetActive(true);
        if (QuestManager.OverEvents.Contains(questEventData))
            gameObject.SetActive(false);
    }
}
