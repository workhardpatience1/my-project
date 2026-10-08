using UnityEngine;

// Guide 1, Part 5.6: hidden and without gravity at first, then it appears and falls.
// Two small fixes:
//  - Time.timeSinceLevelLoad instead of Time.time, so it also works after "Play again"
//    (Time.time is NOT reset when the scene reloads);
//  - optional "wake distance": on a long level the timer starts when the player comes close.
//  - the exercise from the guide: after it fired, the check is switched off.
public class Dropper : MonoBehaviour
{
    [SerializeField] float timeToWait = 2f;
    [SerializeField] Transform player;
    [SerializeField] float wakeDistance = 0f;

    MeshRenderer myMeshRenderer;
    Rigidbody myRigidBody;
    float wakeTime = -1f;
    bool dropped;

    public void Setup(Transform player, float wakeDistance, float timeToWait)
    {
        this.player = player;
        this.wakeDistance = wakeDistance;
        this.timeToWait = timeToWait;
    }

    void Start()
    {
        myMeshRenderer = GetComponent<MeshRenderer>();
        myRigidBody = GetComponent<Rigidbody>();
        myMeshRenderer.enabled = false;
        myRigidBody.useGravity = false;
    }

    void Update()
    {
        if (dropped) return;

        if (wakeTime < 0f)
        {
            bool near = player == null || wakeDistance <= 0f ||
                        transform.position.z - player.position.z < wakeDistance;
            if (!near) return;
            wakeTime = Time.timeSinceLevelLoad;
        }

        if (Time.timeSinceLevelLoad - wakeTime > timeToWait)
        {
            myMeshRenderer.enabled = true;
            myRigidBody.useGravity = true;
            dropped = true;
        }
    }
}
