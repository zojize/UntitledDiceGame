using UnityEngine;

public abstract class BossBehaviour : ScriptableObject
{
    public abstract void OnStart(EnemyUnit enemy);
    public abstract void Reset(EnemyUnit enemy);
    public abstract int ModifyDamage(int damage, EnemyUnit enemy);
    public abstract void PerformAction(EnemyUnit enemy);
}