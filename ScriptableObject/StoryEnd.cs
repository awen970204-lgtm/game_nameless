using UnityEngine;

[CreateAssetMenu(menuName = "Event/Story End")]
public class StoryEnd : ScriptableObject
{
    public string endName;
    public Sprite endImage;
    [TextArea]
    public string endIntroduse;
}
