using UnityEngine;
using System.Collections;
public class PlayerHealth : MonoBehaviour, ITakenDamage
{
    [SerializeField] private int maxHp;
    [SerializeField] private int hp;

    public bool isAttacked;
    public bool isAttack { get { return isAttacked; } set { isAttacked = value; } }

    private Animator anim;

    private void Start()
    {
        hp = maxHp;
        anim = GetComponent<Animator>();
    }

    public void TakenDamage(int _amount)
    {
        if (!isAttack)
        {
            anim.SetTrigger("isHurt");
            hp--;
            StartCoroutine(InvincibleCo());
            UIManager.instance.UpdateHp(hp);
            Debug.Log("Player Hurt");

            if (hp <= 0)
            {
                UIManager.instance.GameOverAnimation();
                FindObjectOfType<CameraController>().CameraShake(0.05f);
            }
        }
    }

    IEnumerator InvincibleCo()
    {
        isAttack = true;
        yield return new WaitForSeconds(2.0f);
        isAttack = false;
    }


}
