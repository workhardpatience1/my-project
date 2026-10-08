using UnityEngine;

// An invisible zone across the track at the end of the level: the player reached the finish.
public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" && GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel();
        }
    }
}
