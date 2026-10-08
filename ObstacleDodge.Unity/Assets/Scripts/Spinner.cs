using UnityEngine;

// Guide 1, Part 5.5: spins the object without stopping.
// Here the angles are in degrees PER SECOND and multiplied by Time.deltaTime,
// so the speed is the same on a slow phone and a fast computer
// (the "keeping animation stable" topic from the guide).
public class Spinner : MonoBehaviour
{
    [SerializeField] float xAngle = 0f;
    [SerializeField] float yAngle = 0f;
    [SerializeField] float zAngle = 0f;

    public void SetAngles(float x, float y, float z)
    {
        xAngle = x;
        yAngle = y;
        zAngle = z;
    }

    void Update()
    {
        transform.Rotate(xAngle * Time.deltaTime, yAngle * Time.deltaTime, zAngle * Time.deltaTime);
    }
}
