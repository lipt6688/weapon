using System.Collections.Generic;
using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int minAttack = 5;
    public int maxAttack = 10;
    public float lifeTime = 2f;
    public bool isPiercing = false; // 是否贯穿
    public float knockbackForce = 0f;
    public Color projectileTint = Color.white;

    [Header("Shotgun Falloff")]
    public bool useDamageFalloff = false;
    public float falloffStartRatio = 0.35f;
    public float minDamageMultiplier = 0.55f;

    public GameObject hitEffect;
    public GameObject damageCanvas;

    private List<Collider2D> hitEnemies = new List<Collider2D>();
    private bool destroyedByHit = false;
    private Vector2 spawnPosition;

    private void Start()
    {
        spawnPosition = transform.position;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = projectileTint;
        }

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

                if (useDamageFalloff)
                {
                    float maxTravel = Mathf.Max(0.01f, speed * lifeTime);
                    float startDistance = maxTravel * Mathf.Clamp01(falloffStartRatio);
                    float traveled = Vector2.Distance(spawnPosition, other.transform.position);
                    float t = Mathf.InverseLerp(startDistance, maxTravel, traveled);
                    float damageScale = Mathf.Lerp(1f, Mathf.Clamp(minDamageMultiplier, 0.1f, 1f), t);
                    attackDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * damageScale));
                }

                enemy.TakenDamage(attackDamage);

                if (knockbackForce > 0f)
                {
                    Vector2 pushDir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
                    if (pushDir.sqrMagnitude < 0.001f)
                    {
                        pushDir = transform.right;
                    }

                    Rigidbody2D enemyRb = other.attachedRigidbody;
                    if (enemyRb != null)
                    {
                        enemyRb.velocity += pushDir * knockbackForce;
                    }
                    else if (other.TryGetComponent<Wizard>(out var wizard))
                    {
                        wizard.ApplyExternalKnockback(pushDir, knockbackForce);
                    }
                    else
                    {
                        other.transform.position = (Vector2)other.transform.position + pushDir * (knockbackForce * 0.05f);
                    }
                }

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
