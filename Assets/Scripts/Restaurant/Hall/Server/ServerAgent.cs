using UnityEngine;

public class ServerAgent : MonoBehaviour
{
    [ReadOnly][SerializeField] private string serverName;
    [ReadOnly][SerializeField] private bool isAutoMode;
    [ReadOnly][SerializeField] private float moveSpeed;
    [ReadOnly][SerializeField] private Vector2 iniPosition;
    [ReadOnly][SerializeField] private int Level;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitialServerSetting(ServerData data)
    {
        serverName = data.serverName;
        isAutoMode = data.isAutoMode;
        moveSpeed = data.moveSpeed;
        iniPosition = data.iniPosition;
        Level = data.level;
    }
}
