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
        Cannon = 5,
        Shotgun = 6
    }

    [Header("武器测试模式 (0全开, 1步枪, 2突刺, 3加特林, 4击退, 5大炮, 6喷子)")]
    public WeaponTestMode testMode = WeaponTestMode.All;
    public KeyCode allWeaponsKey = KeyCode.Alpha0;
    public KeyCode rifleModeKey = KeyCode.Alpha1;
    public KeyCode saberModeKey = KeyCode.Alpha2;
    public KeyCode gatlingModeKey = KeyCode.Alpha3;
    public KeyCode knockbackModeKey = KeyCode.Alpha4;
    public KeyCode cannonModeKey = KeyCode.Alpha5;
    public KeyCode shotgunModeKey = KeyCode.Alpha6;

    [Header("0. 主武器: 固定方向突刺剑 (外形使用光剑)")]
    public GameObject saberVisual;
    public Sprite saberWeaponSprite;
    public int saberSortingOrder = 200;
    public float saberAttackRate = 1.0f;
    public float saberAttackRange = 4f;
    public float saberThrustDistance = 2.4f;
    public float saberThrustDuration = 0.22f;
    public int saberMinAttack = 60;
    public int saberMaxAttack = 90;
    public float saberHitRadius = 0.9f;

    private float nextSaberTime;
    private bool isSaberThrusting = false;
    private float saberThrustTimer = 0f;
    private readonly List<Collider2D> saberHitEnemies = new List<Collider2D>();
    private Vector2 saberAttackDirection = Vector2.right;

    [Header("1. 步枪 (狙击枪: 高伤害/低射速)")]
    public GameObject rifleVisual;
    public float rifleAttackRate = 1.6f;
    [Tooltip("步枪的最远射程限制")]
    public float rifleAttackRange = 18f;
    public float rifleBulletSpeed = 45f;
    public int rifleMinDamage = 100;
    public int rifleMaxDamage = 120;
    public GameObject rifleBulletPrefab;
    private float nextRifleTime;

    [Header("3. 大炮 (右键指哪打哪, AOE)")]
    public GameObject cannonVisual;
    public float cannonCooldown = 3.5f;
    public float cannonAoeRadius = 6.5f;
    public int cannonMinDamage = 200;
    public int cannonMaxDamage = 260;
    public GameObject cannonballPrefab;
    private float nextCannonTime;

    [Header("6. 喷子 (锁定敌群最密集点, 近距离多弹丸, 带击退)")]
    public GameObject shotgunVisual;
    public Sprite shotgunWeaponSprite;
    public float shotgunCooldown = 1.8f;
    public float shotgunRange = 7f;
    public float shotgunClusterRadius = 2.2f;
    public int shotgunPelletCount = 6;
    public float shotgunSpreadAngle = 24f;
    public float shotgunBulletSpeed = 24f;
    public int shotgunMinDamage = 22;
    public int shotgunMaxDamage = 32;
    public float shotgunKnockbackForce = 3.2f;
    public float shotgunVisualScale = 1.0f;
    public Color shotgunProjectileColor = new Color(1f, 0.75f, 0.2f, 1f);
    public GameObject shotgunBulletPrefab;
    private float nextShotgunTime;

    [Header("4. 加特林 (仿步枪: 环绕旋转, 高速低伤)")]
    public GameObject gatlingVisual;
    public float gatlingAttackRate = 20f;
    public float gatlingAttackRange = 8f;
    public float gatlingBulletSpeed = 28f;
    public int gatlingMinDamage = 30;
    public int gatlingMaxDamage = 40;
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
    public int knockbackMinDamage = 40;
    public int knockbackMaxDamage = 60;
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

        if (shotgunVisual == null)
        {
            GameObject generated = new GameObject("ShotgunVisual");
            generated.transform.SetParent(transform, false);
            SpriteRenderer sr = generated.AddComponent<SpriteRenderer>();
            if (shotgunWeaponSprite != null)
            {
                sr.sprite = shotgunWeaponSprite;
            }
            else if (rifleVisual != null)
            {
                SpriteRenderer rifleSr = rifleVisual.GetComponent<SpriteRenderer>();
                if (rifleSr != null)
                {
                    sr.sprite = rifleSr.sprite;
                }
            }

            sr.sortingOrder = saberSortingOrder;
            sr.enabled = true;
            sr.color = Color.white;
            generated.transform.localScale = Vector3.one * shotgunVisualScale;
            shotgunVisual = generated;
        }

        ApplyWeaponSprites();
        EnsureSaberVisualReady();
        EnsureShotgunVisualReady();
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
        bool shotgunEnabled = IsWeaponEnabled(WeaponTestMode.Shotgun);

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

        if (shotgunVisual != null)
        {
            shotgunVisual.SetActive(shotgunEnabled);
            if (shotgunEnabled)
            {
                EnsureShotgunVisualReady();
                UpdateShotgunAttack();
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
        if (Input.GetKeyDown(shotgunModeKey)) SetTestMode(WeaponTestMode.Shotgun);
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

        if (shotgunVisual != null && shotgunWeaponSprite != null)
        {
            SpriteRenderer sr = shotgunVisual.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = shotgunWeaponSprite;
            }
        }
    }

    private void UpdateShotgunAttack()
    {
        if (shotgunVisual == null) return;

        Vector2 aimPoint;
        bool hasTarget = TryGetBestShotgunAimPoint(out aimPoint);
        Vector2 dir = hasTarget ? (aimPoint - (Vector2)centerPos).normalized : GetFixedHorizontalDirection();
        if (dir.sqrMagnitude < 0.001f)
        {
            dir = GetFixedHorizontalDirection();
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        shotgunVisual.transform.position = centerPos + (Vector3)(dir * 0.32f);
        shotgunVisual.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer sr = shotgunVisual.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipY = dir.x < 0f;
        }

        if (!hasTarget || Time.time < nextShotgunTime) return;

        FireShotgunBurst(angle, shotgunVisual.transform.position);
        nextShotgunTime = Time.time + shotgunCooldown;
    }

    private bool TryGetBestShotgunAimPoint(out Vector2 aimPoint)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int bestClusterCount = -1;
        float bestDistanceToPlayer = float.MaxValue;
        Vector2 bestPoint = Vector2.zero;
        bool found = false;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject candidateEnemy = enemies[i];
            if (candidateEnemy == null || !candidateEnemy.activeInHierarchy) continue;

            Vector2 candidatePoint = candidateEnemy.transform.position;
            float distanceToPlayer = Vector2.Distance(centerPos, candidatePoint);
            if (distanceToPlayer > shotgunRange) continue;

            int clusterCount = 0;
            for (int j = 0; j < enemies.Length; j++)
            {
                GameObject nearbyEnemy = enemies[j];
                if (nearbyEnemy == null || !nearbyEnemy.activeInHierarchy) continue;

                if (Vector2.Distance(candidatePoint, nearbyEnemy.transform.position) <= shotgunClusterRadius)
                {
                    clusterCount++;
                }
            }

            if (!found || clusterCount > bestClusterCount || (clusterCount == bestClusterCount && distanceToPlayer < bestDistanceToPlayer))
            {
                found = true;
                bestClusterCount = clusterCount;
                bestDistanceToPlayer = distanceToPlayer;
                bestPoint = candidatePoint;
            }
        }

        aimPoint = bestPoint;
        return found;
    }

    private void EnsureShotgunVisualReady()
    {
        if (shotgunVisual == null) return;

        SpriteRenderer sr = shotgunVisual.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (sr.sprite == null)
        {
            if (shotgunWeaponSprite != null)
            {
                sr.sprite = shotgunWeaponSprite;
            }
            else if (rifleVisual != null)
            {
                SpriteRenderer rifleSr = rifleVisual.GetComponent<SpriteRenderer>();
                if (rifleSr != null)
                {
                    sr.sprite = rifleSr.sprite;
                }
            }
        }

        sr.enabled = true;
        sr.color = Color.white;
        shotgunVisual.transform.localScale = Vector3.one * shotgunVisualScale;
        if (sr.sortingOrder < saberSortingOrder)
        {
            sr.sortingOrder = saberSortingOrder;
        }
    }

    private void FireShotgunBurst(float centerAngle, Vector2 spawnPos)
    {
        GameObject pelletPrefab = shotgunBulletPrefab != null ? shotgunBulletPrefab : rifleBulletPrefab;
        if (pelletPrefab == null) return;

        int pelletCount = Mathf.Max(1, shotgunPelletCount);
        float spreadHalf = shotgunSpreadAngle * 0.5f;

        for (int i = 0; i < pelletCount; i++)
        {
            float t = pelletCount == 1 ? 0.5f : (float)i / (pelletCount - 1);
            float pelletAngle = centerAngle + Mathf.Lerp(-spreadHalf, spreadHalf, t);

            GameObject proj = FireProjectile(pelletPrefab, pelletAngle, spawnPos, shotgunBulletSpeed);
            PlayerProjectile pp = proj.GetComponent<PlayerProjectile>();
            if (pp != null)
            {
                pp.lifeTime = shotgunRange / shotgunBulletSpeed;
                pp.minAttack = shotgunMinDamage;
                pp.maxAttack = shotgunMaxDamage;
                pp.isPiercing = false;
                pp.knockbackForce = shotgunKnockbackForce;
                pp.projectileTint = shotgunProjectileColor;
                pp.useDamageFalloff = true;
                pp.falloffStartRatio = 0.3f;
                pp.minDamageMultiplier = 0.55f;
            }
        }
    }

    private static void ApplyKnockbackToEnemy(Collider2D hit, Vector2 pushDir, float force)
    {
        if (hit == null) return;

        Rigidbody2D enemyRb = hit.attachedRigidbody;
        if (enemyRb != null)
        {
            enemyRb.velocity += pushDir * force;
            return;
        }

        Wizard wizard = hit.GetComponent<Wizard>();
        if (wizard != null)
        {
            wizard.ApplyExternalKnockback(pushDir, force);
            return;
        }

        hit.transform.position = (Vector2)hit.transform.position + pushDir * (force * 0.14f);
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
            cb.minDamage = cannonMinDamage;
            cb.maxDamage = cannonMaxDamage;
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
                ApplyKnockbackToEnemy(hit, pushDir, knockbackPushDistance * 8f);

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
