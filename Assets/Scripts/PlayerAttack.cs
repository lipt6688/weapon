using System.Collections.Generic;
using UnityEngine;

//MARKER This Script has attached to EMPTY GAMEOBJECT
public class PlayerAttack : MonoBehaviour
{
    public enum WeaponTestMode
    {
        All = 0,
        Rifle = 1,
        Saber = 2,
        Gatling = 3,
        Knockback = 4,
        Cannon = 5
    }

    [Header("武器测试模式 (0全开, 1步枪, 2突刺, 3加特林, 4击退, 5大炮)")]
    public WeaponTestMode testMode = WeaponTestMode.All;
    public KeyCode allWeaponsKey = KeyCode.Alpha0;
    public KeyCode rifleModeKey = KeyCode.Alpha1;
    public KeyCode saberModeKey = KeyCode.Alpha2;
    public KeyCode gatlingModeKey = KeyCode.Alpha3;
    public KeyCode knockbackModeKey = KeyCode.Alpha4;
    public KeyCode cannonModeKey = KeyCode.Alpha5;

    [Header("0. 主武器: 固定方向突刺剑 (外形使用光剑)")]
    public GameObject saberVisual;
    public Sprite saberWeaponSprite;
    public int saberSortingOrder = 200;
    public float saberAttackRate = 1.0f;
    public float saberAttackRange = 4f;
    public float saberThrustDistance = 2.4f;
    public float saberThrustDuration = 0.22f;
    public int saberMinAttack = 18;
    public int saberMaxAttack = 30;
    public float saberHitRadius = 0.9f;

    private float nextSaberTime;
    private bool isSaberThrusting = false;
    private float saberThrustTimer = 0f;
    private readonly List<Collider2D> saberHitEnemies = new List<Collider2D>();
    private Vector2 saberAttackDirection = Vector2.right;

    [Header("1. 步枪 (狙击枪: 高伤害/低射速)")]
    public GameObject rifleVisual;
    public float rifleAttackRate = 0.8f;
    [Tooltip("步枪的最远射程限制")]
    public float rifleAttackRange = 18f;
    public float rifleBulletSpeed = 45f;
    public int rifleMinDamage = 70;
    public int rifleMaxDamage = 125;
    public GameObject rifleBulletPrefab;
    private float nextRifleTime;

    [Header("3. 大炮 (右键指哪打哪, AOE)")]
    public GameObject cannonVisual;
    public float cannonCooldown = 3.5f;
    public float cannonAoeRadius = 6.5f;
    public GameObject cannonballPrefab;
    private float nextCannonTime;

    [Header("4. 加特林 (仿步枪: 环绕旋转, 高速低伤)")]
    public GameObject gatlingVisual;
    public float gatlingAttackRate = 20f;
    public float gatlingAttackRange = 8f;
    public float gatlingBulletSpeed = 28f;
    public int gatlingMinDamage = 4;
    public int gatlingMaxDamage = 8;
    public float gatlingOrbitRadius = 1.2f;
    public float gatlingOrbitAngularSpeed = 5f;
    public GameObject gatlingBulletPrefab;
    private float nextGatlingTime;

    [Header("5. 击退武器 (仿 Vampire 击退武器)")]
    public GameObject knockbackWeaponVisual;
    public Sprite knockbackWeaponSprite;
    public float knockbackOrbitRadius = 1.65f;
    public float knockbackRotationSpeed = 220f;
    public float knockbackHitRadius = 0.55f;
    public float knockbackHitCooldown = 0.35f;
    public int knockbackMinDamage = 7;
    public int knockbackMaxDamage = 12;
    public float knockbackPushDistance = 0.45f;

    private float knockbackAngle = 0f;
    private readonly Dictionary<Collider2D, float> knockbackLastHitTime = new Dictionary<Collider2D, float>();

    private Vector3 centerPos;

    private GameObject hitEffectPref;
    private GameObject damageCanvasPref;

    private void Start()
    {
        EnsureSaberVisualReady();
        ApplyWeaponSprites();

        Slash slash = GetComponentInChildren<Slash>(true);
        if (slash != null)
        {
            hitEffectPref = slash.hitEffect;
            damageCanvasPref = slash.damageCanvas;
        }

        Slash[] legacyMelee = GetComponentsInChildren<Slash>(true);
        foreach (Slash melee in legacyMelee)
        {
            melee.gameObject.SetActive(false);
        }

        if (knockbackWeaponVisual == null)
        {
            GameObject generated = new GameObject("KnockbackWeaponVisual");
            generated.transform.SetParent(transform, false);
            SpriteRenderer sr = generated.AddComponent<SpriteRenderer>();
            if (saberVisual != null)
            {
                SpriteRenderer saberSr = saberVisual.GetComponent<SpriteRenderer>();
                if (saberSr != null)
                {
                    sr.sprite = knockbackWeaponSprite != null ? knockbackWeaponSprite : saberSr.sprite;
                    sr.sortingOrder = saberSr.sortingOrder;
                    generated.transform.localScale = saberVisual.transform.localScale * 0.7f;
                }
            }
            knockbackWeaponVisual = generated;
        }

        ApplyWeaponSprites();
        EnsureSaberVisualReady();
    }

    private void Update()
    {
        HandleTestModeInput();

        centerPos = transform.parent != null ? transform.parent.position : transform.position;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        bool saberEnabled = IsWeaponEnabled(WeaponTestMode.Saber);
        bool rifleEnabled = IsWeaponEnabled(WeaponTestMode.Rifle);
        bool cannonEnabled = IsWeaponEnabled(WeaponTestMode.Cannon);
        bool gatlingEnabled = IsWeaponEnabled(WeaponTestMode.Gatling);
        bool knockbackEnabled = IsWeaponEnabled(WeaponTestMode.Knockback);

        if (saberEnabled)
        {
            if (saberVisual != null) saberVisual.SetActive(true);
            EnsureSaberVisualReady();
            UpdateSaberAttack();
        }
        else
        {
            if (saberVisual != null) saberVisual.SetActive(false);
            isSaberThrusting = false;
            saberThrustTimer = 0f;
            saberHitEnemies.Clear();
        }

        bool isCannonReady = Time.time >= nextCannonTime;
        if (cannonVisual != null) cannonVisual.SetActive(cannonEnabled && isCannonReady);

        if (cannonEnabled && isCannonReady && Input.GetMouseButtonDown(1))
        {
            FireCannon(mouseWorldPos);
            nextCannonTime = Time.time + cannonCooldown;
            if (cannonVisual != null) cannonVisual.SetActive(false);
        }

        if (rifleVisual != null)
        {
            rifleVisual.SetActive(rifleEnabled);
            if (rifleEnabled)
            {
                Vector2 viewDir = mouseWorldPos - centerPos;
                float lookAngle = Mathf.Atan2(viewDir.y, viewDir.x) * Mathf.Rad2Deg;

                rifleVisual.transform.position = centerPos + (Vector3)(viewDir.normalized * 0.2f);
                rifleVisual.transform.rotation = Quaternion.Euler(0, 0, lookAngle);
                SpriteRenderer rifleSr = rifleVisual.GetComponent<SpriteRenderer>();
                if (rifleSr != null)
                {
                    // Keep rifle orientation consistent when aiming to the left side.
                    rifleSr.flipY = viewDir.x < 0f;
                }

                if (Input.GetMouseButtonDown(0) && Time.time >= nextRifleTime)
                {
                    if (rifleBulletPrefab != null)
                    {
                        GameObject proj = FireProjectile(rifleBulletPrefab, lookAngle, rifleVisual.transform.position, rifleBulletSpeed);
                        PlayerProjectile pp = proj.GetComponent<PlayerProjectile>();
                        if (pp != null)
                        {
                            pp.lifeTime = rifleAttackRange / rifleBulletSpeed;
                            pp.minAttack = rifleMinDamage;
                            pp.maxAttack = rifleMaxDamage;
                        }
                        nextRifleTime = Time.time + 1f / rifleAttackRate;
                    }
                }
            }
        }

        if (knockbackEnabled)
        {
            if (knockbackWeaponVisual != null) knockbackWeaponVisual.SetActive(true);
            UpdateKnockbackWeapon();
        }
        else if (knockbackWeaponVisual != null)
        {
            knockbackWeaponVisual.SetActive(false);
        }

        if (gatlingVisual != null)
        {
            gatlingVisual.SetActive(gatlingEnabled);
            if (gatlingEnabled)
            {
                float theta = Time.time * gatlingOrbitAngularSpeed;
                Vector2 gunDir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                float angle = Mathf.Atan2(gunDir.y, gunDir.x) * Mathf.Rad2Deg;

                gatlingVisual.transform.position = centerPos + (Vector3)(gunDir * gatlingOrbitRadius);
                gatlingVisual.transform.rotation = Quaternion.Euler(0, 0, angle);

                if (Time.time >= nextGatlingTime && gatlingBulletPrefab != null)
                {
                    GameObject proj = FireProjectile(gatlingBulletPrefab, angle, gatlingVisual.transform.position, gatlingBulletSpeed);
                    PlayerProjectile pp = proj.GetComponent<PlayerProjectile>();
                    if (pp != null)
                    {
                        pp.lifeTime = gatlingAttackRange / gatlingBulletSpeed;
                        pp.minAttack = gatlingMinDamage;
                        pp.maxAttack = gatlingMaxDamage;
                    }
                    nextGatlingTime = Time.time + 1f / gatlingAttackRate;
                }
            }
        }
    }

    private void HandleTestModeInput()
    {
        if (Input.GetKeyDown(allWeaponsKey)) SetTestMode(WeaponTestMode.All);
        if (Input.GetKeyDown(rifleModeKey)) SetTestMode(WeaponTestMode.Rifle);
        if (Input.GetKeyDown(saberModeKey)) SetTestMode(WeaponTestMode.Saber);
        if (Input.GetKeyDown(gatlingModeKey)) SetTestMode(WeaponTestMode.Gatling);
        if (Input.GetKeyDown(knockbackModeKey)) SetTestMode(WeaponTestMode.Knockback);
        if (Input.GetKeyDown(cannonModeKey)) SetTestMode(WeaponTestMode.Cannon);
    }

    private void SetTestMode(WeaponTestMode mode)
    {
        if (testMode == mode) return;
        testMode = mode;
        Debug.Log("Weapon Test Mode: " + mode);
    }

    private bool IsWeaponEnabled(WeaponTestMode mode)
    {
        return testMode == WeaponTestMode.All || testMode == mode;
    }

    private void UpdateSaberAttack()
    {
        if (saberVisual == null) return;

        saberAttackDirection = GetFixedHorizontalDirection();

        float angle = Mathf.Atan2(saberAttackDirection.y, saberAttackDirection.x) * Mathf.Rad2Deg;
        float baseRadius = 0.8f;
        float currentRadius = baseRadius;

        if (isSaberThrusting)
        {
            saberThrustTimer += Time.deltaTime;
            float fraction = Mathf.Clamp01(saberThrustTimer / saberThrustDuration);
            currentRadius = baseRadius + Mathf.Sin(fraction * Mathf.PI) * saberThrustDistance;

            DamageEnemiesInSaberPath();

            if (saberThrustTimer >= saberThrustDuration)
            {
                isSaberThrusting = false;
                saberThrustTimer = 0f;
                saberHitEnemies.Clear();
            }
        }
        else if (Time.time >= nextSaberTime)
        {
            isSaberThrusting = true;
            saberThrustTimer = 0f;
            saberHitEnemies.Clear();
            nextSaberTime = Time.time + 1f / saberAttackRate;
        }

        UpdateWeaponTransform(saberVisual, angle, currentRadius);
    }

    private Vector2 GetFixedHorizontalDirection()
    {
        Transform owner = transform.parent != null ? transform.parent : transform;
        float yRotation = owner.eulerAngles.y;
        bool facingLeft = yRotation > 90f && yRotation < 270f;
        return facingLeft ? Vector2.left : Vector2.right;
    }

    private void ApplyWeaponSprites()
    {
        if (saberVisual != null && saberWeaponSprite != null)
        {
            SpriteRenderer sr = saberVisual.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = saberWeaponSprite;
            }
        }

        if (knockbackWeaponVisual != null && knockbackWeaponSprite != null)
        {
            SpriteRenderer sr = knockbackWeaponVisual.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = knockbackWeaponSprite;
            }
        }
    }

    private void EnsureSaberVisualReady()
    {
        if (saberVisual == null) return;

        SpriteRenderer sr = saberVisual.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (saberWeaponSprite != null)
        {
            sr.sprite = saberWeaponSprite;
        }

        sr.enabled = true;
        sr.color = Color.white;
        if (sr.sortingOrder < saberSortingOrder)
        {
            sr.sortingOrder = saberSortingOrder;
        }
    }

    private void FireCannon(Vector3 targetLocation)
    {
        if (cannonballPrefab == null) return;

        Vector3 spawnPos = targetLocation + new Vector3(0, 20f, 0);
        GameObject cbObj = Instantiate(cannonballPrefab, spawnPos, Quaternion.identity);
        Cannonball cb = cbObj.GetComponent<Cannonball>();
        if (cb != null)
        {
            cb.targetPos = targetLocation;
            cb.aoeRadius = cannonAoeRadius;
            cb.boomEffect = hitEffectPref;
            cb.damageCanvas = damageCanvasPref;
        }
    }

    private void DamageEnemiesInSaberPath()
    {
        if (saberVisual == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(saberVisual.transform.position, saberHitRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") && !saberHitEnemies.Contains(hit))
            {
                saberHitEnemies.Add(hit);
                ITakenDamage enemy = hit.GetComponent<ITakenDamage>();
                if (enemy != null && !enemy.isAttack)
                {
                    int dmg = Random.Range(saberMinAttack, saberMaxAttack);
                    enemy.TakenDamage(dmg);

                    if (hitEffectPref != null) Instantiate(hitEffectPref, hit.transform.position, Quaternion.identity);
                    if (damageCanvasPref != null)
                    {
                        DamageNum damagable = Instantiate(damageCanvasPref, hit.transform.position, Quaternion.identity).GetComponent<DamageNum>();
                        damagable.ShowDamage(dmg);
                    }
                }
            }
        }
    }

    private void UpdateKnockbackWeapon()
    {
        if (knockbackWeaponVisual == null) return;

        knockbackAngle += knockbackRotationSpeed * Time.deltaTime;
        if (knockbackAngle > 360f) knockbackAngle -= 360f;

        float rad = knockbackAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        knockbackWeaponVisual.transform.position = centerPos + (Vector3)(dir * knockbackOrbitRadius);
        knockbackWeaponVisual.transform.rotation = Quaternion.Euler(0f, 0f, knockbackAngle + 90f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(knockbackWeaponVisual.transform.position, knockbackHitRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (!hit.CompareTag("Enemy")) continue;

            float lastHitTime;
            if (knockbackLastHitTime.TryGetValue(hit, out lastHitTime) && Time.time - lastHitTime < knockbackHitCooldown)
            {
                continue;
            }

            ITakenDamage enemy = hit.GetComponent<ITakenDamage>();
            if (enemy != null && !enemy.isAttack)
            {
                int dmg = Random.Range(knockbackMinDamage, knockbackMaxDamage);
                enemy.TakenDamage(dmg);

                Vector2 pushDir = ((Vector2)hit.transform.position - (Vector2)centerPos).normalized;
                Rigidbody2D enemyRb = hit.attachedRigidbody;
                if (enemyRb != null)
                {
                    enemyRb.velocity += pushDir * (knockbackPushDistance * 8f);
                }
                else
                {
                    hit.transform.position = (Vector2)hit.transform.position + pushDir * knockbackPushDistance;
                }

                if (hitEffectPref != null)
                {
                    Instantiate(hitEffectPref, hit.transform.position, Quaternion.identity);
                }

                if (damageCanvasPref != null)
                {
                    DamageNum damagable = Instantiate(damageCanvasPref, hit.transform.position, Quaternion.identity).GetComponent<DamageNum>();
                    damagable.ShowDamage(dmg);
                }

                knockbackLastHitTime[hit] = Time.time;
            }
        }
    }

    private void UpdateWeaponTransform(GameObject weapon, float angle, float radius)
    {
        if (weapon == null) return;

        float rad = angle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        weapon.transform.position = centerPos + (Vector3)offset;

        weapon.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private GameObject FireProjectile(GameObject prefab, float angle, Vector2 initPosition, float flySpeed = 10f)
    {
        GameObject proj = Instantiate(prefab, initPosition, Quaternion.Euler(0, 0, angle));
        PlayerProjectile pp = proj.GetComponent<PlayerProjectile>();
        if (pp != null)
        {
            pp.speed = flySpeed;
        }
        return proj;
    }

    public Transform GetClosestEnemy(float maxRange)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closest = null;
        float minDistance = maxRange;

        foreach (var enemy in enemies)
        {
            if (enemy.activeInHierarchy)
            {
                float dist = Vector2.Distance(centerPos, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = enemy.transform;
                }
            }
        }
        return closest;
    }
}
