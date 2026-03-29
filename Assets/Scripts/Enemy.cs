using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, ITakenDamage
{
    [SerializeField] private float moveSpeed;
    private Transform target;
    [SerializeField] private int maxHp;
    public int hp;

    [Header("Hurt")]
    private SpriteRenderer sp;
    public float hurtLength;//MARKER 效果持续多久
    private float timeBtwHurt;//MARKER 相当于计数器

    [HideInInspector] public bool isAttacked;
    [SerializeField] private GameObject explosionEffect;

    public bool isAttack { get { return isAttacked; }  set { isAttacked = value; } }

    public GameObject bulletEffect;

    private void Start() 
    {
        hp = maxHp;
        target = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        sp = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        FollowPlayer();

        timeBtwHurt -= Time.deltaTime;
        if (timeBtwHurt <= 0)
            sp.material.SetFloat("_FlashAmount", 0);
    }

    private void FollowPlayer()
    {
        transform.position = Vector2.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
    }

    public void TakenDamage(int _amount)
    {
        isAttack = true;
        StartCoroutine(isAttackCo());
        hp -= _amount;
        HurtEffect();

        if (hp <= 0)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    private void HurtEffect() 
    {
        sp.material.SetFloat("_FlashAmount", 1);
        timeBtwHurt = hurtLength;
    }

    IEnumerator isAttackCo()
    {
        yield return new WaitForSeconds(0.2f);
        isAttack = false;
    }

}
