using UnityEngine;
using System.Collections.Generic;

public enum TowerType { SingleTarget, MultiTarget }

public class Tower : MonoBehaviour
{
    [Header("Тип башни")]
    [SerializeField] private TowerType towerType = TowerType.SingleTarget;
    [SerializeField] private int maxTargets = 3; 

    [Header("Настройки атаки")]
    [SerializeField] private float attackRadius = 15f;
    [SerializeField] private float startTimeBtwAttack = 1f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform shootPoint;

    [Header("Ссылки")]

    private GameManager gameManager;
    private MovementMobs targetEnemy;
    private float timeBtwAttack;

    public GameObject nextTower;
    public int cost;

    [Header("Desert Effects")]
    [SerializeField] private float desertSpeedMultiplier = 0.25f;
    [SerializeField] private float desertFireRateMultiplier = 1.35f;
    [SerializeField] private LayerMask oasisLayer;
    [SerializeField] private float oasisCheckRadius = 3f;

    private float originalFireRate;
    private float originalBulletSpeed;
    private bool isInDesert = false;
    private bool hasOasisNearby = false;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip shootSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        gameManager = FindObjectOfType<GameManager>();
        originalFireRate = startTimeBtwAttack;

        if (bulletPrefab != null && bulletPrefab.TryGetComponent<Bullet>(out var bullet))
        {
            originalBulletSpeed = bullet.speed;
        }

        isInDesert = gameManager != null && gameManager.currentLevelType == GameManager.LevelType.Desert;
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        if (targetEnemy == null)
        {
            MovementMobs nearest = GetNearestEnemy();
            if (nearest != null)
            {
                targetEnemy = nearest;
            }
        }
        if (targetEnemy != null && Vector3.Distance(transform.position, targetEnemy.transform.position) > attackRadius)
        {
            targetEnemy = null;
        }
        timeBtwAttack -= Time.deltaTime;
        if (timeBtwAttack <= 0 && targetEnemy != null)
        {
            timeBtwAttack = startTimeBtwAttack;
            if (targetEnemy != null && targetEnemy.gameObject.activeInHierarchy)
            {
                Shoot();
            }
        }

        if (!isInDesert) return;

        CheckForOasis();
        ApplyCurrentEffects();
    }

    private void CheckForOasis()
    {
        bool newOasisState = Physics.CheckSphere(transform.position, oasisCheckRadius, oasisLayer);

        if (newOasisState != hasOasisNearby)
        {
            hasOasisNearby = newOasisState;
            ApplyCurrentEffects();
        }
    }

    private void ApplyCurrentEffects()
    {
        if (!isInDesert)
        {
            startTimeBtwAttack = originalFireRate;
            return;
        }

        if (hasOasisNearby)
        {
            startTimeBtwAttack = originalFireRate;
        }
        else
        {
            startTimeBtwAttack = originalFireRate * desertFireRateMultiplier;
        }
    }



    private List<MovementMobs> GetEnemiesInRange()
    {
        List<MovementMobs> enemiesInRange = new List<MovementMobs>();
        foreach (MovementMobs enemy in gameManager.EnemyList)
        {
            if(enemy != null && Vector3.Distance(transform.position, enemy.transform.position) <= attackRadius)
            {
                enemiesInRange.Add(enemy);
            }
        }
        return enemiesInRange;
    }

    private MovementMobs GetNearestEnemy()
    {
        MovementMobs nearestEnemy = null;
        float smallesDistance = float.PositiveInfinity;
        foreach(MovementMobs enemy in GetEnemiesInRange())
        {
            if (Vector3.Distance(transform.position, enemy.transform.position) < smallesDistance)
            {
                smallesDistance = Vector3.Distance(transform.position, enemy.transform.position);
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

   

    private void Shoot()
    {
        if (bulletPrefab == null || shootPoint == null) return;

        if (towerType == TowerType.SingleTarget)
        {
            FireBullet(targetEnemy);
        }
        else if (towerType == TowerType.MultiTarget)
        {
            List<MovementMobs> enemiesInRange = GetEnemiesInRange();
            int targetsToShoot = Mathf.Min(maxTargets, enemiesInRange.Count);

            for (int i = 0; i < targetsToShoot; i++)
            {
                if (i < enemiesInRange.Count)
                {
                    FireBullet(enemiesInRange[i]);
                }
            }
        }
    }

    private void FireBullet(MovementMobs enemy)
    {
        if (GameManager.Instance != null && Time.timeScale == 0) return;

        GameObject newBullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);
        Bullet bulletComponent = newBullet.GetComponent<Bullet>();

        if (bulletComponent != null)
        {
            bulletComponent.target = enemy;
            bulletComponent.tower = this;

            if (isInDesert && !hasOasisNearby)
            {
                bulletComponent.speed = originalBulletSpeed * desertSpeedMultiplier;
            }
            else
            {
                bulletComponent.speed = originalBulletSpeed;
            }
        }
        else
        {
            Debug.LogError("У префаба снаряда нет компонента Bullet!");
        }

        if (shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        if (isInDesert)
        {
            Gizmos.color = hasOasisNearby ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, oasisCheckRadius);
        }
    }
}
