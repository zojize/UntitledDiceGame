using UnityEngine;

public enum effect {DAMAGE, MOD, HEAL}

public class Dice : MonoBehaviour
{
    public int maxDamage;
    public int maxMod;
    public int maxHeal;

    public int currDamage;
    public int currMod;
    public int currHeal;

    // TODO:
    // makes one of curr to a value
    
    void Awake() {
        currMod = 1;
        // random for now
        effect randomEffect = (effect)Random.Range(0, System.Enum.GetValues(typeof(effect)).Length);
        
        switch(randomEffect) {
            case effect.DAMAGE:
                currDamage = Random.Range(4, maxDamage+1);
                break;
            case effect.HEAL:
                currHeal = Random.Range(2, maxHeal+1);
                break;
            case effect.MOD:
                currMod = Random.Range(1, maxMod+1);
                break;
        }
    }
}
