using UnityEngine;

[CreateAssetMenu(fileName = "Enrage", menuName = "EnemyBehaviours/Enrage")]
public class Enrage : BossBehaviour
{
    public float damageThresholdFactor;
    public float damageIncreaseFactor;
    
    public override void OnStart(EnemyUnit enemy)
    {
    }

    public override void Reset(EnemyUnit enemy)
    {
        OnStart(enemy);
    }

    public override int ModifyDamage(int damage, EnemyUnit enemy)
    {
        return damage;
    }

    public override void PerformAction(EnemyUnit enemy)
    {
        if (enemy.currHP < enemy.maxHP*damageThresholdFactor) {
            Debug.Log($"Enemy Enraged: {enemy.currDamage} to {(int) (enemy.currDamage*damageIncreaseFactor)}" );
            enemy.currDamage = (int) (enemy.currDamage*damageIncreaseFactor);
        }
    }
}
