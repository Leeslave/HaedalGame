using UnityEngine;

public enum CameraZone
{
    Restaurant = 50,
    PreOperation = 0,
    PostOperation = -50
}

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    private CameraZone curZone;
    private Vector3 targetPosition;

    void Awake()
    {
        Instance = this;
        targetPosition = transform.position;    
    }

    public void MoveToZone(CameraZone zone)
    {
        curZone = zone;
        targetPosition = new Vector3(transform.position.x, (float)zone, transform.position.z);
        transform.position = targetPosition;
    }

    public CameraZone GetCurZone() { return curZone; }

    
}