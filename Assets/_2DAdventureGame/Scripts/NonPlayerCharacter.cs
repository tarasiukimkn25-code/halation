using UnityEngine;

public class NonPlayerCharacter : MonoBehaviour
{
    public GameObject dialogueBubble;
    public string encyclopediaFactId;

    void Awake()
    {
        if (dialogueBubble != null)
            dialogueBubble.SetActive(false);
    }
}
