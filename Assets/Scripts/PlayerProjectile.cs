using System.Collections.Generic;
using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int minAttack = 5;
    public int maxAttack = 10;
    public float lifeTime = 2f;
    public bool isPiercing = false; // 是否贯穿

    public GameObject hitEffect;
    public GameObject damageCanvas;

    private List<Collider2D> hitEnemies = new List<Collider2D>();
    private bool destroyedByHit = false;

    private void Start()
    {
        // 自动销毁
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        Vector2 start = transform.position;
        Vector2 end = start + (Vector2)(transform.right * speed * Time.deltaTime);

        if (!TrySweepHit(start, end))
        {
            transform.position = end;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other);
    }

    private bool TrySweepHit(Vector2 start, Vector2 end)
    {
        RaycastHit2D[] hits = Physics2D.LinecastAll(start, end);
        if (hits == null || hits.Length == 0) return false;

        float nearestDistance = float.MaxValue;
        Collider2D nearestCollider = null;
        Vector2 nearestPoint = end;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D collider = hits[i].collider;
            if (collider == null) continue;

            if (!collider.CompareTag("Enemy") && !collider.CompareTag("Wall")) continue;

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                nearestCollider = collider;
                nearestPoint = hits[i].point;
            }
        }

        if (nearestCollider == null) return false;

        transform.position = nearestPoint;
        HandleCollision(nearestCollider);
        return destroyedByHit;
    }

    private void HandleCollision(Collider2D other)
    {
        if (destroyedByHit || other == null) return;

        if (other.CompareTag("Enemy") && !hitEnemies.Contains(other))
        {
            hitEnemies.Add(other); // 防止单次投射物重复伤害同一敌人

            ITakenDamage enemy = other.GetComponent<ITakenDamage>();
            if (enemy != null && !enemy.isAttack)
            {
                int attackDamage = Random.Range(minAttack, maxAttack);
                enemy.TakenDamage(attackDamage);

                // 播放命中特效
                if (hitEffect != null)
                {
                    Instantiate(hitEffect, transform.position, Quaternion.identity);
                }

                // 显示伤害数字
                if (damageCanvas != null)
                {
                    DamageNum damagable = Instantiate(damageCanvas, other.transform.position, Quaternion.identity).GetComponent<DamageNum>();
                    damagable.ShowDamage(attackDamage);
                }

                if (!isPiercing)
                {
                    destroyedByHit = true;
                    Destroy(gameObject); // 不贯穿的话命中即销毁
                }
            }
        }
        else if (other.CompareTag("Wall"))
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            destroyedByHit = true;
            Destroy(gameObject);
        }
    }
}
