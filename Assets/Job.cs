// Job.cs - 별도 파일로 생성

public enum JobType
{
    Sheriff,      // 보안관
    Zombie,       // 좀비
    Martyr,       // 순교자
    Executioner,  // 처형자
    Vampire,      // 흡혈귀
    SpeedShooter, // 스피드슈터
    Hacker,       // 해커
    Doctor,       // 의사
    Joker         // 광대
}

public enum SkillType
{
    Passive,
    Active
}

[System.Serializable]
public class Job
{
    public JobType Type;
    public string Name;
    public string Description;
    public SkillType SkillType;
    public bool IsUsed; // 액티브 스킬 사용 여부

    public static Job Create(JobType type)
    {
        return type switch
        {
            JobType.Sheriff => new Job
            {
                Type = type,
                Name = "보안관",
                Description = "치명타 턴 모든 데미지 무효, HP 1 생존 (1회)",
                SkillType = SkillType.Passive
            },
            JobType.Zombie => new Job
            {
                Type = type,
                Name = "좀비",
                Description = "죽는 턴 킬 성공 시 HP 4 부활",
                SkillType = SkillType.Passive
            },
            JobType.Martyr => new Job
            {
                Type = type,
                Name = "순교자",
                Description = "사망 시 전체 2뎀 + 타이브레이커 시 자동 승리",
                SkillType = SkillType.Passive
            },
            JobType.Executioner => new Job
            {
                Type = type,
                Name = "처형자",
                Description = "HP 3 이하 적 공격 시 +2뎀",
                SkillType = SkillType.Passive
            },
            JobType.Vampire => new Job
            {
                Type = type,
                Name = "흡혈귀",
                Description = "단독 킬 시 HP 2 회복",
                SkillType = SkillType.Passive
            },
            JobType.SpeedShooter => new Job
            {
                Type = type,
                Name = "스피드슈터",
                Description = "재장전 + 발포 동시 (1회)",
                SkillType = SkillType.Active
            },
            JobType.Hacker => new Job
            {
                Type = type,
                Name = "해커",
                Description = "타겟 이번 턴 행동 무효 (1회)",
                SkillType = SkillType.Active
            },
            JobType.Doctor => new Job
            {
                Type = type,
                Name = "의사",
                Description = "HP 3 회복 (1회)",
                SkillType = SkillType.Active
            },
            JobType.Joker => new Job
            {
                Type = type,
                Name = "광대",
                Description = "첫 사망자 + 그 턴 미공격 시 단독 승리",
                SkillType = SkillType.Passive
            },
            _ => null
        };
    }
}