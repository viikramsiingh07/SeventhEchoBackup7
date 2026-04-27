using UnityEngine;

public class PlayerDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Player has Animator: " + (GetComponent<Animator>() != null));
        Debug.Log("Player has SpriteRenderer: " + (GetComponent<SpriteRenderer>() != null));
        Debug.Log("Child Animators: " + GetComponentsInChildren<Animator>(true).Length);
        Debug.Log("Child Renderers: " + GetComponentsInChildren<SpriteRenderer>(true).Length);
    }
}
