using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum combatState { START, PLAYER, ENEMY, WON, LOST }

public class CombatSystem : MonoBehaviour
{
    public GameObject player;
    public GameObject enemy;

    public CombatHUD playerHUD;
    public CombatHUD enemyHUD;
    public TMP_Text playerDamage;
    public TMP_Text enemyDamage;

    public TMP_Text logText;
    public GameObject gameOverPage;
    public GameObject playerEffect;
    public GameObject enemyEffect;

    // public DiceManager diceManager;
    public bool liveDice = true;


    // For future spawning
    // public Transform playerLoc;
    // public Transform enemyLoc;

    private combatState state;

    Unit playerUnit;
    Unit enemyUnit;
    EffectAnimation playerEffectController;
    EffectAnimation enemyEffectController;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameOverPage.SetActive(false);
        state = combatState.START;
        playerUnit = player.GetComponent<Unit>();
        enemyUnit = enemy.GetComponent<Unit>();
        playerEffectController = playerEffect.GetComponent<EffectAnimation>();
        enemyEffectController = enemyEffect.GetComponent<EffectAnimation>();
        DiceManager.OnEndSimulationEvent += onReceiveDiceInfo;
        SetUpCombat();
    }

    void SetUpCombat()
    {
        // Not spawning different enemies yet
        // Intantiate(player, playerLoc);
        // Intantiate(enemy, enemyLoc);

        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);
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

    public void onReceiveDiceInfo(List<Die> dice)
    {
        if (liveDice) {
            if (state != combatState.PLAYER)
                return;

            StartCoroutine(PlayerAction());
        }
    }

    IEnumerator PlayerAction()
    {
        if (liveDice) {
            int damage = 0;

            List<int> diceValues = DiceManager._desiredSides;
            for (int i = 0; i < diceValues.Count; i++) {
                damage += diceValues[i];
            }

            Debug.Log($"Damage: {damage}");
            PlayerAttack(damage, 1);
            yield return new WaitForSeconds(1.5f);
        } else {
            // calculate damage and hp
            int tempDamage = 0;
            int tempHP = 0;
            int tempMod = 1;
            GameObject dicePrefab = Resources.Load<GameObject>("Prefab/Dice");
            for (int i = 0; i < 3; i++)
            {
                GameObject dice = Instantiate(dicePrefab);
                Dice diceInfo = dice.GetComponent<Dice>();

                tempDamage += diceInfo.currDamage;
                tempHP += diceInfo.currHeal;
                tempMod *= diceInfo.currMod;
                // Destroy(dice);
            }
            Debug.Log((tempDamage, tempHP, tempMod));

            if (tempHP != 0)
            {
                PlayerHeal(tempHP, tempMod);
                yield return new WaitForSeconds(1.5f);
            }

            if (tempHP == 0 && tempDamage == 0)
            {
                // mod being taken as damage
                tempDamage = tempMod;
                tempMod = 1;
            }

            if (tempDamage != 0)
            {
                PlayerAttack(tempDamage, tempMod);
                yield return new WaitForSeconds(1.5f);
            }
        }

        if (state == combatState.WON)
        {
            EndCombat();
        }
        else
        {
            StartCoroutine(EnemyTurn());
        }
    }

    void PlayerAttack(int damage, int mod)
    {
        // TODO: spawning effect
        int finalDamage = damage * mod;

        bool isDead = enemyUnit.TakeDamage(finalDamage);

        enemyDamage.text = "-" + finalDamage;
        enemyEffectController.PlayDamageEffect();

        enemyHUD.SetHP(enemyUnit.currHP);
        logText.text = "The attack is successful! -" + damage + " x" + mod;

        if (isDead)
        {
            state = combatState.WON;
        }
    }

    void PlayerHeal(int hp, int mod)
    {
        // TODO: spawning effect

        int finalHp = hp * mod;

        playerUnit.Heal(finalHp);

        playerHUD.SetHP(playerUnit.currHP);

        playerEffectController.PlayHealEffect();

        logText.text = "You feel renewed strength! +" + hp + " x"
        + mod;
    }

    IEnumerator EnemyTurn()
    {
        int action = Random.Range(0, 2);
        bool isDead = false;

        if (action == 0)
        {
            logText.text = "Enemy attacks!";
            isDead = playerUnit.TakeDamage(enemyUnit.damage);
            playerDamage.text = "-" + enemyUnit.damage;
            playerEffectController.PlayDamageEffect();
            playerHUD.SetHP(playerUnit.currHP);
        }
        else
        {
            logText.text = "Enemy gains more strength.";
            enemyUnit.Heal(5);
            enemyEffectController.PlayHealEffect();
            enemyHUD.SetHP(enemyUnit.currHP);
        }

        yield return new WaitForSeconds(1.5f);

        if (isDead)
        {
            state = combatState.LOST;
            EndCombat();
        }
        else
        {
            state = combatState.PLAYER;
            logText.text = "Your turn.";
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
    }

    public void OnClickRestart()
    {
        logText.text = "";
        gameOverPage.SetActive(false);
        state = combatState.START;
        playerUnit.Reset();
        enemyUnit.Reset();
        playerHUD.SetHP(playerUnit.currHP);
        enemyHUD.SetHP(enemyUnit.currHP);
        state = combatState.PLAYER;
    }
}
