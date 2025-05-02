using UnityEngine;
using System.Collections;

public class EffectAnimation : MonoBehaviour
{
    public GameObject heal;
    public string healName;
    public GameObject damage;
    public string damageName;

    Animator healAnimator;
    Animator damageAnimator;

    void Awake() {
        heal.SetActive(false);
        damage.SetActive(false);
        healAnimator = heal.GetComponent<Animator>();
        damageAnimator = damage.GetComponent<Animator>();
    }

    public void PlayHealEffect()
    {
        heal.SetActive(true);
        healAnimator.Play(healName);
        StartCoroutine(DisableEffect(heal, healAnimator));
    }

    public void PlayDamageEffect()
    {
        damage.SetActive(true);
        damageAnimator.Play(damageName);
        StartCoroutine(DisableEffect(damage, damageAnimator));
    }

    IEnumerator DisableEffect(GameObject effect,Animator anim)
    {
        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
        effect.SetActive(false);
    }
}
