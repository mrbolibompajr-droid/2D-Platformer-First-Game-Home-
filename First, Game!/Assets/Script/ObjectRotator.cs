using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    [SerializeField] private Vector3 objectRotation;
    [SerializeField] private float rotationSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(objectRotation * rotationSpeed * Time.deltaTime);
    }
}
