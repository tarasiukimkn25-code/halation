using UnityEngine;

public class NonPlayerCharacter : MonoBehaviour
{
    public GameObject dialogueBubble;
    public string encyclopediaFactId;

    void Start()
    {
        if (dialogueBubble != null)
            dialogueBubble.SetActive(false);
    }
}
