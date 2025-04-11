using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

public class EnemyUnit : Unit
{
    // public Dice [] inventory;
    public int[] damage;
    public int[] heal;
    public int[] mod;

    public List<BossBehaviour> bossBehaviours = new List<BossBehaviour>();
    private List<BossBehaviour> runtimeBehaviours = new List<BossBehaviour>();

    public GameObject healEffect;
    public GameObject attackEffect;
    public TMP_Text effectText; // temp

    public int currDamage;
    public int currHeal;

    protected override void Start() {
        healEffect.SetActive(false);
        attackEffect.SetActive(false);
        effectText.text = "";

        foreach (var behaviour in bossBehaviours)
        {
            var clone = ScriptableObject.Instantiate(behaviour);
            clone.OnStart(this);
            runtimeBehaviours.Add(clone);
        }

        // foreach (var behaviour in bossBehaviours)
        // {
        //     behaviour = Instantiate(behaviour);
        //     behaviour.OnStart(this);
        // }
        base.Start();

        CombatSystem.OnBeginPlayerTurn += NextTurnAction;
        CombatSystem.OnBeginEnemyTurn += ActionTurn;
    }

    public void NextTurnAction() {
        // get all three randomized values
        // set the private vals
        int action = UnityEngine.Random.Range(0, 2);
        int modN = mod[UnityEngine.Random.Range(0, mod.Length)];
        if (action == 0) { // damage
            currDamage = damage[UnityEngine.Random.Range(0, damage.Length)] * modN;
            currHeal = 0;
            // spawning symbol
            attackEffect.SetActive(true);
        } else {
            currHeal = heal[UnityEngine.Random.Range(0, heal.Length)] * modN;
            currDamage = 0;
            // spawning symbol
            healEffect.SetActive(true);
        }
    }

    public override bool TakeDamage(int damage) {
        foreach (var behaviour in runtimeBehaviours)
        {
            damage = behaviour.ModifyDamage(damage, this);
        }

        return base.TakeDamage(damage);
    }

    public void ActionTurn() {
        // remove symbol
        healEffect.SetActive(false);
        attackEffect.SetActive(false);

        foreach (var behaviour in runtimeBehaviours)
        {
            behaviour.PerformAction(this);
        }
    }
    
    public override void Reset() {
        healEffect.SetActive(false);
        attackEffect.SetActive(false);

        foreach (var behaviour in runtimeBehaviours)
        {
            behaviour.Reset(this);
        }

        base.Reset();
    }
}

