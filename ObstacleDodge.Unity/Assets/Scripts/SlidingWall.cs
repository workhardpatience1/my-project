using UnityEngine;

// An extra obstacle for later levels: a wall that slides left and right.
public class SlidingWall : MonoBehaviour
{
    [SerializeField] float amplitude = 4f;
    [SerializeField] float speed = 1.5f;
    [SerializeField] float phase = 0f;
    float baseX;

    public void Setup(float amplitude, float speed, float phase)
    {
        this.amplitude = amplitude;
        this.speed = speed;
        this.phase = phase;
    }

    void Start()
    {
        baseX = transform.position.x;
    }

    void Update()
    {
        Vector3 p = transform.position;
        p.x = baseX + amplitude * Mathf.Sin(Time.timeSinceLevelLoad * speed + phase);
        transform.position = p;
    }
}
