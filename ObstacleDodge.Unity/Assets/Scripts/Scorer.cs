using UnityEngine;

// Guide 1, Part 4.3. Kept for learning only: the ads guide (Part 2.5) moves hit counting
// to ObjectHit + GameManager, so LevelBuilder does NOT put Scorer on the player.
public class Scorer : MonoBehaviour
{
    int hits = 0;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag != "Hit")
        {
            hits++;
            Debug.Log("You've bumped into a thing this many times: " + hits);
        }
    }
}
