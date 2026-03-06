using UnityEngine;

public class GotchaManager : MonoBehaviour
{
    public static GotchaManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    

}
