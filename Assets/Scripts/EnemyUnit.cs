using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using System.Linq;


public class EnemyUnit : Unit
{
    // public Dice [] inventory;
    public int[] damage;
    public int[] heal;
    public int[] mod;

    private GameObject damageDie;
    private GameObject healDie;
    private GameObject modDie;

    public List<BossBehaviour> bossBehaviours = new List<BossBehaviour>();
    private List<BossBehaviour> runtimeBehaviours = new List<BossBehaviour>();

    public GameObject healEffect;
    public GameObject attackEffect;
    public TMP_Text effectText; // temp

    public int currDamage;
    public int currHeal;

    private GameObject _diePrefab;
    private GameObject _tilePrefab;
    private List<Die> diceInventory;
    private DieFaceType action;

    protected override void Start() {
        _diePrefab = Resources.Load<GameObject>("Prefabs/Die");
        _tilePrefab = Resources.Load<GameObject>("Prefabs/BlueprintTile");
        healEffect.SetActive(false);
        attackEffect.SetActive(false);
        effectText.text = "";

        damageDie = GenerateDice(DieFaceType.Damage);
        healDie = GenerateDice(DieFaceType.Heal);
        modDie = GenerateDice(DieFaceType.Multiplier);
        // GenerateDice();

        foreach (var behaviour in bossBehaviours)
        {
            var clone = ScriptableObject.Instantiate(behaviour);
            clone.OnStart(this);
            runtimeBehaviours.Add(clone);
        }

        base.Start();

        CombatSystem.OnBeginPlayerTurn += NextTurnAction;
        CombatSystem.OnBeginEnemyTurn += ActionTurn;
    }

    public void NextTurnAction() {
        // get all three randomized values
        // set the private vals

        damageDie.SetActive(false);
        modDie.SetActive(false);
        healDie.SetActive(false);
        currHeal = 0;
        currDamage = 0;

        action = (DieFaceType) UnityEngine.Random.Range(0, 2);
        if (action == DieFaceType.Damage) { // damage
            // spawning symbol
            attackEffect.SetActive(true);
        } else {
            // spawning symbol
            healEffect.SetActive(true);
        }
        // int modN = mod[UnityEngine.Random.Range(0, mod.Length)];
        // if (action == DieFaceType.Damage) { // damage
        //     currDamage = damage[UnityEngine.Random.Range(0, damage.Length)] * modN;
        //     currHeal = 0;
        //     // spawning symbol
        //     attackEffect.SetActive(true);
        // } else {
        //     currHeal = heal[UnityEngine.Random.Range(0, heal.Length)] * modN;
        //     currDamage = 0;
        //     // spawning symbol
        //     healEffect.SetActive(true);
        // }
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

        // List<Die> playerSelectedDice = new List<Die>(DiceManager.SelectedDice);
        // DiceManager.SelectedDice.Clear();
        List<Die> enemyDice = new List<Die>();
        List<int> desiredSides = new List<int>();
        desiredSides.Add(UnityEngine.Random.Range(1, 7));
        desiredSides.Add(UnityEngine.Random.Range(1, 7));

        // activate the dice
        modDie.SetActive(true);
        // DiceManager.SelectedDice.Add(modDie.GetComponent<Die>());
        enemyDice.Add(modDie.GetComponent<Die>());
        if (action == DieFaceType.Damage) { 
            damageDie.SetActive(true);

            // DiceManager.SelectedDice.Add(damageDie.GetComponent<Die>());
            // DiceManager.BeginSimulation();
            // List<Die> dice = DiceManager.SelectedDice;
            // var sides = dice.Select(d => d.GetTopSide()).ToList();
            // int modN = modDie.GetComponent<Die>().GetFace((Side)sides[0]).Value;
            // currDamage = damageDie.GetComponent<Die>().GetFace((Side)sides[1]).Value * modN;

            enemyDice.Add(damageDie.GetComponent<Die>());
            List<int> rolledValues = DiceManager.RollDice(enemyDice, desiredSides);
            int modN = rolledValues[0];
            currDamage = rolledValues[1] * modN;
        } else {
            healDie.SetActive(true);

            // DiceManager.SelectedDice.Add(healDie.GetComponent<Die>());
            // DiceManager.BeginSimulation();
            // List<Die> dice = DiceManager.SelectedDice;
            // var sides = dice.Select(d => d.GetTopSide()).ToList();
            // int modN = modDie.GetComponent<Die>().GetFace((Side)sides[0]).Value;
            // currDamage = healDie.GetComponent<Die>().GetFace((Side)sides[1]).Value * modN;

            enemyDice.Add(healDie.GetComponent<Die>());
            List<int> rolledValues = DiceManager.RollDice(enemyDice, desiredSides);
            int modN = rolledValues[0];
            currHeal = rolledValues[1] * modN;
        }

        // DiceManager.SelectedDice.Clear();
        // DiceManager.SelectedDice.AddRange(playerSelectedDice);

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

    public GameObject GenerateDice(DieFaceType type) {
        GameObject die = Instantiate(_diePrefab);
        var dieComponent = die.GetComponent<Die>();

        for (int i = 1; i < 7; i++)
        {
            int value = 0;
            Texture texture = null;
            switch (type) {
                case DieFaceType.Damage:
                    value = UnityEngine.Random.Range(1, 11);
                    texture = Resources.Load<Texture>($"Textures/Numbers/RED_{value}");
                    break;
                case DieFaceType.Heal:
                    value = UnityEngine.Random.Range(1, 11);
                    texture = Resources.Load<Texture>($"Textures/Numbers/GREEN_{value}");
                    break;
                case DieFaceType.Multiplier:
                    value = UnityEngine.Random.Range(1, 5); // not allowing large mod values
                    texture = Resources.Load<Texture>($"Textures/Numbers/PURPLE_{value}");
                    break;
                default:
                    break;
            }
            DieFace face = new DieFace(type, value, texture);
            dieComponent.TrySetFace((Side)i, face);
        }

        die.transform.position = new Vector3(20, -10, 0); // set below the enemy
        die.SetActive(false);
        return die;
    }
}

