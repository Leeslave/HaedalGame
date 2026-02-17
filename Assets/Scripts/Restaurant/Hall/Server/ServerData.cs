using UnityEngine;

[CreateAssetMenu(fileName = "NewServerData", menuName = "Restaurant/ServerData")]
public class ServerData : ScriptableObject
{
    public string serverName;
    public bool isAutoMode;
    public float moveSpeed;
    public Vector2 iniPosition;
    public int level;
}
