using UnityEngine;

// A simple "Cinemachine": the camera follows the player smoothly from behind and above.
public class FollowCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 9.5f, -11.5f);
    [SerializeField] float lookAhead = 4.5f;
    [SerializeField] float smooth = 6f;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target == null) return;
        transform.position = DesiredPosition();
        transform.LookAt(LookPoint());
    }

    Vector3 Focus() => new Vector3(target.position.x * 0.55f, 0.6f, target.position.z);
    Vector3 DesiredPosition() => Focus() + offset;
    Vector3 LookPoint() => Focus() + new Vector3(0f, 0f, lookAhead);

    void LateUpdate()
    {
        if (target == null) return;
        float k = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, DesiredPosition(), k);
        transform.LookAt(LookPoint());
    }
}
