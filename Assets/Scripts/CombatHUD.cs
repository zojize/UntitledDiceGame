using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatHUD : MonoBehaviour
{
    // If ever needed
    // public Text nameText;
	// public Text levelText;
	public Slider hpSlider;
	public TMP_Text totalHP;
	public TMP_Text currentHP;

	public void SetHUD(Unit unit)
	{
		hpSlider.maxValue = unit.maxHP;
		hpSlider.value = unit.currHP;
		totalHP.text = unit.maxHP.ToString();
		currentHP.text = unit.currHP.ToString();
	}

	public void SetHP(int hp)
	{
		hpSlider.value = hp;
		currentHP.text = hp.ToString();
	}
}
