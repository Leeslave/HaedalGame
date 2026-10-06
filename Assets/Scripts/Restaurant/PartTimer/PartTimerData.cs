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
    public int appearanceSeed;
    public float stamina = -1;
    public string instanceId;       // 보유 알바 개체 ID (세이브 식별용, 이름·등급이 같아도 구분)
    public string serverName;       // 서빙 알바의 이름
    public string level;               // 서빙 알바의 등급
    public PartTimerStatus status;     // 서빙 알바의 status
    public int wage;
    public PartTimerRole CurrentRole = PartTimerRole.None;
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

    public EmployeeEntry ToEntry(int slotIndex)
    {
        return new EmployeeEntry
        {
            appearanceSeed = appearanceSeed,
            stamina = stamina,
            instanceId = instanceId,
            name = serverName,
            grade = level,
            serving = status != null ? status.serving : 0f,
            cooking = status != null ? status.cooking : 0f,
            handy = status != null ? status.handy : 0f,
            hp = status != null ? status.hp : 0f,
            wage = wage,
            role = CurrentRole.ToString(),
            slotIndex = CurrentRole == PartTimerRole.None ? -1 : slotIndex,
        };
    }

    public static PartTimerData FromEntry(EmployeeEntry entry)
    {
        PartTimerData data = new PartTimerData();
        data.appearanceSeed = entry.appearanceSeed;
        data.stamina = entry.stamina;
        data.instanceId = entry.instanceId;
        data.serverName = entry.name;
        data.level = entry.grade;
        data.wage = entry.wage;
        data.status = new PartTimerStatus
        {
            serving = entry.serving,
            cooking = entry.cooking,
            handy = entry.handy,
            hp = entry.hp,
        };
        data.CurrentRole = System.Enum.TryParse(entry.role, out PartTimerRole role) ? role : PartTimerRole.None;
        return data;
    }
}
