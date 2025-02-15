using UnityEngine;
using System.Collections;

public class EffectAnimation : MonoBehaviour
{
    Animator animator;
    public GameObject effect;
    public string animationName;

    void Awake() {
        effect.SetActive(false);
        animator = effect.GetComponent<Animator>();
    }

    public void PlayEffect()
    {
        effect.SetActive(true);
        animator.Play(animationName);
        StartCoroutine(DisableHealEffect());
    }

    IEnumerator DisableHealEffect()
    {
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        effect.SetActive(false);
    }
}
