using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;
public class ItemControl : MonoBehaviour
{
    [HideInInspector] public Item item;

    [SerializeField] private Image itemPicter;
    [SerializeField] private TMP_Text itemName;
    
    void Start()
    {
        if (item != null)
        {
            itemPicter.sprite = item.itemPicture;
            itemName.text = item.itemName;
        }
    }

    public void SetUp(Item target)
    {
        item = target;
        itemPicter.sprite = target.itemPicture;
        itemName.text = target.itemName;
    }
}
