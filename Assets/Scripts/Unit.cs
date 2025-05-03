using UnityEngine;
using TMPro;

public class Unit : MonoBehaviour
{
    public int maxHP;
    public int currHP;
	public CombatHUD hud;
	public GameObject animEffect;
	public TMP_Text damageEffect;
	private EffectAnimation effectController;

 	protected virtual void Start() {
		hud.SetHUD(this);
		effectController = animEffect.GetComponent<EffectAnimation>();
	}

    public virtual bool TakeDamage(int dmg)
	{
		currHP -= dmg;
		damageEffect.text = "-" + dmg;
		effectController.PlayDamageEffect();

		if (currHP <= 0) {
			currHP = 0;
			hud.SetHP(currHP);
			return true;
		} else {
			hud.SetHP(currHP);
			return false;
		}
	}

	public void Heal(int amount)
	{
		currHP += amount;
		if (currHP > maxHP)
			currHP = maxHP;
		hud.SetHP(currHP);
		effectController.PlayHealEffect();
	}

	public virtual void Reset()
	{
		currHP = maxHP;
		hud.SetHP(currHP);
	}
}