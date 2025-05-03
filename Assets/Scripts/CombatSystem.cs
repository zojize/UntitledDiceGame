using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public enum combatState { START, PLAYER, ENEMY, WON, LOST }

public class CombatSystem : MonoBehaviour
{
    public GameObject player;
    public GameObject enemy;

    public static event Action OnBeginPlayerTurn; 
    // public event Action OnEndPlayerTurn;
    public static event Action OnBeginEnemyTurn;
    // public event Action OnEndEnemyTurn;
    public static event Action OnEndCombat;

    public TMP_Text logText;
    public GameObject gameOverPage;

    public bool liveDice = true;

    private Unit playerUnit;
    private EnemyUnit enemyUnit;

    // For future spawning
    // public Transform playerLoc;
    // public Transform enemyLoc;

    private combatState state;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameOverPage.SetActive(false);
        state = combatState.START;
        playerUnit = player.GetComponent<Unit>();
        enemyUnit = enemy.GetComponent<EnemyUnit>();

        DiceManager.OnEndSimulationEvent += OnReceiveDiceInfo;
        CombatSystem.OnBeginEnemyTurn += OnEnemyTurn;
        CombatSystem.OnBeginPlayerTurn += OnPlayerTurn;
        CombatSystem.OnEndCombat += EndCombat;

        SetUpCombat();
    }

    void SetUpCombat()
    {
        // Not spawning different enemies yet
        // Intantiate(player, playerLoc);
        // Intantiate(enemy, enemyLoc);

        CombatSystem.OnBeginPlayerTurn?.Invoke();
    }

    void OnPlayerTurn() {
        logText.text = "Your turn.";
        state = combatState.PLAYER;
    }

    public void OnUseButton()
    {
        if (!liveDice) {
            if (state != combatState.PLAYER)
                return;

            StartCoroutine(PlayerAction());
        }
    }

    public void OnReceiveDiceInfo(List<Die> dice)
    {
        if (liveDice) {
            if (state != combatState.PLAYER)
                return;

            StartCoroutine(PlayerAction());
        }
    }

    IEnumerator PlayerAction()
    {
        int damage = 0;
        int heal = 0;
        int mod = 1;

        if (liveDice) {
            List<Die> dice = DiceManager.SelectedDice;
            var sides = dice.Select(d => d.GetTopSide()).ToList();
            for (int i = 0; i < dice.Count; i++) {
                IDieFace face = dice[i].GetFace((Side)sides[i]);
                if (face.Type == DieFaceType.Damage) {
                    damage += face.Value;
                } else if (face.Type == DieFaceType.Heal) {
                    heal += face.Value;
                } else if (face.Type == DieFaceType.Multiplier) {
                    mod *= face.Value;
                }     
            }
        } else {
            GameObject dicePrefab = Resources.Load<GameObject>("Prefabs/Dice");
            for (int i = 0; i < 3; i++)
            {
                GameObject dice = Instantiate(dicePrefab);
                Dice diceInfo = dice.GetComponent<Dice>();

                damage += diceInfo.currDamage;
                heal += diceInfo.currHeal;
                mod *= diceInfo.currMod;
                // Destroy(dice);
            }
        }
        Debug.Log((damage, heal, mod));
        
        if (heal != 0)
        {
            PlayerHeal(heal, mod);
            yield return new WaitForSeconds(1.5f);
        }

        if (heal == 0 && damage == 0)
        {
            // mod being taken as damage
            damage = mod;
            mod = 1;
        }

        if (damage != 0)
        {
            PlayerAttack(damage, mod);
            yield return new WaitForSeconds(1.5f);
        }

        if (state == combatState.WON)
        {
            CombatSystem.OnEndCombat?.Invoke();
        }
        else
        {
            CombatSystem.OnBeginEnemyTurn?.Invoke();
        }
    }

    void PlayerAttack(int damage, int mod)
    {
        // TODO: spawning effect
        int finalDamage = damage * mod;
        bool isDead = enemyUnit.TakeDamage(finalDamage);
        logText.text = "The attack is successful! -" + damage + " x" + mod;

        if (isDead)
        {
            state = combatState.WON;
        }
    }

    void PlayerHeal(int hp, int mod)
    {
        int finalHp = hp * mod;

        playerUnit.Heal(finalHp);
        logText.text = "You feel renewed strength! +" + hp + " x" + mod;
    }

    void OnEnemyTurn() {
        state = combatState.ENEMY;
        StartCoroutine(EnemyTurn());
    }

    IEnumerator EnemyTurn()
    {
        bool isDead = false;

        if (enemyUnit.currDamage != 0) {
            logText.text = "Enemy attacks!";
            Debug.Log($"Enemy Damage: {enemyUnit.currDamage}" );
            isDead = playerUnit.TakeDamage(enemyUnit.currDamage);
        }
        else {
            logText.text = "Enemy gains more strength.";
            Debug.Log($"Enemy Heal: {enemyUnit.currHeal}" );
            enemyUnit.Heal(enemyUnit.currHeal);
        }

        yield return new WaitForSeconds(1.5f);

        if (isDead)
        {
            state = combatState.LOST;
            CombatSystem.OnEndCombat?.Invoke();
        }
        else
        {
            CombatSystem.OnBeginPlayerTurn?.Invoke();
        }

    }

    void EndCombat()
    {
        gameOverPage.SetActive(true);
        if (state == combatState.WON)
        {
            logText.text = "You won the battle!";
        }
        else if (state == combatState.LOST)
        {
            logText.text = "You were defeated.";
        }

        // when no restart
        // CombatSystem.OnBeginPlayerTurn -= NextTurnAction;
        // CombatSystem.OnBeginEnemyTurn -= DamageAction;
    }

    // will be gone
    public void OnClickRestart()
    {
        logText.text = "";
        gameOverPage.SetActive(false);
        state = combatState.START;
        playerUnit.Reset();
        enemyUnit.Reset();
        CombatSystem.OnBeginPlayerTurn?.Invoke();
    }
}
