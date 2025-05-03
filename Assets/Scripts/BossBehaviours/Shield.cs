using UnityEngine;

[CreateAssetMenu(fileName = "Shield", menuName = "EnemyBehaviours/Shield")]
public class Shield : BossBehaviour
{
    public int damageThreshold;
    public float damageReduceFactor;
    public int shieldRound;
    private bool shieldOn;
    private int shieldOnRound;
    private int damageTaken;

    public override void OnStart(EnemyUnit enemy)
    {
        damageTaken = 0;
        shieldOn = false;
        shieldOnRound = 0;
    }

    public override void Reset(EnemyUnit enemy)
    {
        OnStart(enemy);
    }

    public override int ModifyDamage(int damage, EnemyUnit enemy)
    {
        if (shieldOn) {
            shieldOnRound--;
            if (shieldOnRound == 0) {
                shieldOn = false;
                enemy.effectText.text = "";
            }
            Debug.Log($"Enemy Shield On: {damage} to {(int) (damageReduceFactor*damage)}" );
            return (int) (damageReduceFactor*damage);
        } else {
            damageTaken += damage;
            Debug.Log($"Enemy Damage Taken: {damageTaken}");
            if (damageTaken > damageThreshold) {
                shieldOn = true;
                enemy.effectText.text = "Shield";
                shieldOnRound = shieldRound;
                damageTaken = 0;
            }
            return damage;
        }
    }

    // public void OnDamageTaken(EnemyUnit enemy)
    // {
        
    // }

    public override void PerformAction(EnemyUnit enemy)
    {
        // Debug.Log($"{enemy.name} is executing boss-specific action!");
        // Define attack patterns or special moves here
    }
}
