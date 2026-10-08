using UnityEngine;

// Guide 1, Part 5.7: sleeps (switched off in Awake) until a trap wakes it up,
// then flies to the spot where the player stood at that moment and destroys itself there.
public class FlyAtPlayer : MonoBehaviour
{
    [SerializeField] float speed = 1.0f;
    [SerializeField] Transform player;
    Vector3 playerPosition;

    public void Setup(Transform player, float speed)
    {
        this.player = player;
        this.speed = speed;
    }

    void Awake()
    {
        gameObject.SetActive(false);
    }

    void Start()
    {
        playerPosition = player.transform.position;
    }

    void Update()
    {
        MoveToPlayer();
        DestroyWhenReached();
    }

    void MoveToPlayer()
    {
        transform.position =
            Vector3.MoveTowards(transform.position, playerPosition, Time.deltaTime * speed);
    }

    void DestroyWhenReached()
    {
        if (transform.position == playerPosition)
        {
            Destroy(gameObject);
        }
    }
}
