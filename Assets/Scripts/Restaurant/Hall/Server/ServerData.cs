[System.Serializable]
public class ServerStatus
{
    public float speed; // 이동 속도
    public float attraction; // 매력
    public float hp; // 체력
}

public class ServerData
{
    public string serverName;       // 서빙 알바의 이름
    public int level;               // 서빙 알바의 등급
    public ServerStatus status;     // 서빙 알바의 status

    public void ServerStatusInit()  // 처음 가챠 등으로 생성 시 실행하여 해당 레벨에 맞는 스탯을 설정해야함
    {
        // 값 랜덤 설정
        switch(level)
        {
            case 0:
                // 노말 스탯 적용 총합 300
                break;
            case 1:
                // 실버 스탯 적용 총합 400
                break;
            case 2:
                // 골드 스탯 적용 총합 500
                break;    
        }
    }
}
