using UnityEngine;

public class Unit : MonoBehaviour
{
    public int damage;

    public int maxHP;
    public int currHP;

    public bool TakeDamage(int dmg)
	{
		currHP -= dmg;

		if (currHP <= 0) {
			currHP = 0;
			return true;
		}
		else {
			return false;
		}
	}

	public void Heal(int amount)
	{
		currHP += amount;
		if (currHP > maxHP)
			currHP = maxHP;
	}

    // // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Start()
    // {
        
    // }

    // // Update is called once per frame
    // void Update()
    // {
        
    // }
}
