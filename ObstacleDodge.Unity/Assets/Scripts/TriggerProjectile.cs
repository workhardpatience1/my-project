using UnityEngine;

// Guide 1, Part 5.8, written as the exercise suggests: one array instead of five fields,
// so a trap can have any number of projectiles.
public class TriggerProjectile : MonoBehaviour
{
    [SerializeField] GameObject[] projectiles;

    public void Setup(GameObject[] projectiles)
    {
        this.projectiles = projectiles;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            foreach (GameObject projectile in projectiles)
            {
                if (projectile != null) projectile.SetActive(true);
            }
            Destroy(gameObject);
        }
    }
}
