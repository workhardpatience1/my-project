using UnityEngine;

// Guide 1, Part 5.1 + ads guide, Part 2.5:
// the first time the player touches this obstacle, a hit is counted in GameManager,
// then the obstacle turns black and gets the "Hit" tag, so it is never counted again.
public class ObjectHit : MonoBehaviour
{
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (gameObject.tag != "Hit" && GameManager.Instance != null)
            {
                GameManager.Instance.RegisterHit();
            }
            GetComponent<MeshRenderer>().material.color = Color.black;
            gameObject.tag = "Hit";
        }
    }
}
