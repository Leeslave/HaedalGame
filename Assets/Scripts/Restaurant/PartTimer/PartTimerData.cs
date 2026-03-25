public enum PartTimerRole
{
    None,
    Serving,
    Kitchen
}
[System.Serializable]

public class PartTimerStatus
{
    public float serving; // 서빙 속도
    public float cooking; // 요리 속도
    public float handy;   // 손재주 -> 팁 받을 확률 증가, 요리 등급업 증가
    public float hp;      // 체력
}

public class PartTimerData
{
    public string serverName;       // 서빙 알바의 이름
    public string level;               // 서빙 알바의 등급
    public PartTimerStatus status;     // 서빙 알바의 status
    public int wage;
    public void ServerStatusInit()  // 처음 가챠 등으로 생성 시 실행하여 해당 레벨에 맞는 스탯을 설정해야함
    {
        // 값 랜덤 설정
        switch(level)
        {
            case "F":
                wage = 350;
                break;
            case "E":
                wage = 700;
                break;
            case "D":
                wage = 1200;
                break;
            case "C":
                wage = 2500;
                break;
            case "B":
                wage = 5000;
                break;
            case "A":
                wage = 10000;
                break;
        }
    }
}
