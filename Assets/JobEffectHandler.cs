using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class JobEffectHandler
{
    // ========== 데미지 계산 전 (처형자) ==========
    public static int ModifyDamage(Job attackerJob, int baseDamage, int targetHP)
    {
        if (attackerJob == null) return baseDamage;

        if (attackerJob.Type == JobType.Executioner && targetHP <= 3)
        {
            Debug.Log("<color=orange>처형자 보너스! +2뎀</color>");
            return baseDamage + 2;
        }
        
        return baseDamage;
    }

    // ========== 사망 판정 (보안관) ==========
    public static bool CheckSheriffSurvival(Job job, ref int newHP)
    {
        if (job == null) return false;
        if (job.Type != JobType.Sheriff) return false;
        if (job.IsUsed) return false;
        if (newHP > 0) return false;

        job.IsUsed = true;
        newHP = 1;
        Debug.Log("<color=cyan>보안관 스킬 발동! HP 1로 생존</color>");
        return true;
    }

    // ========== 좀비 부활 체크 ==========
    public static bool CheckZombieRevival(Job job, bool gotKillThisTurn, ref int newHP)
    {
        if (job == null) return false;
        if (job.Type != JobType.Zombie) return false;
        if (newHP > 0) return false;
        if (!gotKillThisTurn) return false;

        newHP = 4;
        Debug.Log("<color=green>좀비 부활! HP 4</color>");
        return true;
    }

    // ========== 순교자 사망 효과 ==========
    public static void ApplyMartyrEffect(Job job, ulong victimId, Dictionary<ulong, int> hpMap)
    {
        if (job == null) return;
        if (job.Type != JobType.Martyr) return;

        Debug.Log("<color=red>순교자 사망! 전체 2뎀</color>");
        var keys = hpMap.Keys.ToList();
        foreach (var playerId in keys)
        {
            if (playerId != victimId)
            {
                hpMap[playerId] = Mathf.Max(0, hpMap[playerId] - 2);
            }
        }
    }

    // ========== 흡혈귀 회복 ==========
    public static void ApplyVampireHeal(Job job, ulong killerId, bool isSoloKill, 
                                         Dictionary<ulong, int> hpMap, int maxHP)
    {
        if (job == null) return;
        if (job.Type != JobType.Vampire) return;
        if (!isSoloKill) return;

        int newHP = Mathf.Min(maxHP, hpMap[killerId] + 2);
        hpMap[killerId] = newHP;
        Debug.Log("<color=red>흡혈귀 회복! +2 HP</color>");
    }

    // ========== 해커 행동 무효 ==========
    public static bool IsBlockedByHacker(ulong targetId, ulong hackerId, ulong hackerTargetId, 
                                          Job hackerJob, bool hackerUsedSkillThisTurn)
    {
        if (hackerJob == null) return false;
        if (hackerJob.Type != JobType.Hacker) return false;
        if (hackerJob.IsUsed) return false;
        if (!hackerUsedSkillThisTurn) return false;
        if (hackerTargetId != targetId) return false;

        return true;
    }

    // ========== 광대 승리 체크 ==========
    public static bool CheckJokerWin(Job job, bool isFirstDeath, bool didNotAttack)
    {
        if (job == null) return false;
        if (job.Type != JobType.Joker) return false;
        if (!isFirstDeath) return false;
        if (!didNotAttack) return false;

        Debug.Log("<color=magenta>광대 승리!</color>");
        return true;
    }

    // ========== 순교자 타이브레이커 승리 ==========
    public static bool CheckMartyrTiebreakerWin(Job job)
    {
        if (job == null) return false;
        if (job.Type != JobType.Martyr) return false;

        Debug.Log("<color=magenta>순교자 타이브레이커 승리!</color>");
        return true;
    }

    // ========== 의사 회복 (액티브) ==========
    public static void UseDoctorSkill(Job job, ulong userId, Dictionary<ulong, int> hpMap, int maxHP)
    {
        if (job == null) return;
        if (job.Type != JobType.Doctor) return;
        if (job.IsUsed) return;

        job.IsUsed = true;
        int newHP = Mathf.Min(maxHP, hpMap[userId] + 3);
        hpMap[userId] = newHP;
        Debug.Log("<color=green>의사 스킬! +3 HP</color>");
    }

    // ========== 스피드슈터 체크 ==========
    public static bool CanSpeedShooterReloadAndFire(Job job)
    {
        if (job == null) return false;
        if (job.Type != JobType.SpeedShooter) return false;
        if (job.IsUsed) return false;
        return true;
    }

    public static void MarkSkillUsed(Job job)
    {
        if (job != null) job.IsUsed = true;
    }
}