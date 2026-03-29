using UnityEngine;

public class Cannonball : MonoBehaviour
{
    [HideInInspector] public Vector3 targetPos;
    public float speed = 30f; // 迫击炮下落速度加快
    public float aoeRadius = 6.5f; // 稍微缩小爆炸范围
    public int minDamage = 150; // 更高伤害
    public int maxDamage = 300; 

    public GameObject boomEffect;
    public GameObject damageCanvas;

    private void Start()
    {
        // 迫击炮逻辑：立刻把炮弹传送到目标正上方高空，然后垂直下坠，而不是从玩家发射
        transform.position = targetPos + new Vector3(0, 20f, 0);
    }

    private void Update()
    {
        // 向目标点飞行
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // 如果距离目标点非常近了，就引爆
        if (Vector3.Distance(transform.position, targetPos) <= 0.05f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // AOE 圆形范围伤害判定
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                ITakenDamage enemy = hit.GetComponent<ITakenDamage>();
                if (enemy != null && !enemy.isAttack)
                {
                    int dmg = Random.Range(minDamage, maxDamage);
                    enemy.TakenDamage(dmg);
                    
                    if (damageCanvas != null)
                    {
                        DamageNum damagable = Instantiate(damageCanvas, hit.transform.position, Quaternion.identity).GetComponent<DamageNum>();
                        damagable.ShowDamage(dmg);
                    }
                }
            }
        }

        // 播放爆炸特效
        if (boomEffect != null)
        {
            Instantiate(boomEffect, transform.position, Quaternion.identity);
        }

        // 销毁炮弹
        Destroy(gameObject);
    }
    
    // 在编辑器中绘制出爆炸范围，方便你看到AOE多大
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}
