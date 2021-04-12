﻿using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    #region Variables
    #region Components
    [Header("Components")]
    [SerializeField] private Animator enemyAnim;
    [SerializeField] private Collider2D enemyCol;
    [SerializeField] private AudioSource enemyAudioSource;
    #endregion

    #region Animator
    [Header("Animator References")]
    [SerializeField] private string enemyAnim_DeathTrigger;
    private int enemyAnim_DeathTriggerHash => Animator.StringToHash(enemyAnim_DeathTrigger);
    #endregion

    #region Enemy Properties
    [Header("Enemy Properties")]
    [SerializeField] private int entityHealth;
    public int EntityHealth { get { return entityHealth; } set { entityHealth = value; } }

    [SerializeField] private int entityMaxHealth;
    public int EntityMaxHealth { get { return entityMaxHealth; } set { entityMaxHealth = value; } }

    [SerializeField] private float enemySpeed = 4f;

    [Header("Enemy Shoot")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private Vector3 laserSpawnOffset;
    [SerializeField] private Vector2 fireRate = new Vector2(2f, 4f);
    private float fireRateTimer;

    [SerializeField] [Range(0, 30)] private int enemyProjectileLayer;
    [SerializeField] private bool isVisible;

    [Header("Score")]
    [SerializeField] private int scoreToGive;

    [Header("Audio")]
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip explosionClip;
    #endregion

    #region Events
    // Events
    public event System.Action<int> OnEntityDamaged;
    public event System.Action<IDamageable> OnEntityKilled;
    #endregion
    #endregion


    #region MonoBehaviour Methods
    private void Start()
    {
        entityHealth = entityMaxHealth;
    }

    // Update is called once per frame
    private void Update()
    {
        Move();
        Shoot();
    }

    private void OnBecameVisible()
    {
        isVisible = true;
        fireRateTimer = Time.time + Random.Range(fireRate.x, fireRate.y) * Random.Range(0f, .4f);
    }
    private void OnBecameInvisible()
    {
        isVisible = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IDamageable entityCollided))
        {
            entityCollided.TakeDamage(1);
            Explode();
        }
    }
    #endregion

    #region Custom Methods
    private void Move()
    {
        // Move downwards
        transform.Translate(Vector3.down * enemySpeed * Time.deltaTime);

        // If the enemy is out of bounds, tp it above the screen in a new X position
        if (transform.position.y <= SpaceShooterData.EnemyBoundLimitsY.x)
        {
            transform.position = new Vector3(Random.Range(-SpaceShooterData.EnemySpawnX, SpaceShooterData.EnemySpawnX), SpaceShooterData.EnemyBoundLimitsY.y, transform.position.z);
        }
    }
    private void Shoot()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.IsGameOver()) return;
        }

        if (isVisible && enemyCol.enabled)
        {
            if (Time.time >= fireRateTimer)
            {
                // Update timer
                fireRateTimer = Time.time + Random.Range(fireRate.x, fireRate.y);

                // Ignore own lasers
                Transform[] lasersToIgnore = Instantiate(laserPrefab, transform.position + laserSpawnOffset, Quaternion.identity).GetComponentsInChildren<Transform>();
                for (int i = 0; i < lasersToIgnore.Length; i++)
                {
                    lasersToIgnore[i].gameObject.layer = enemyProjectileLayer;
                }

                // Play Shoot soundclip
                AudioManager.Instance?.PlayOneShotClip(shootClip);
            }
        }
    }

    public void TakeDamage(int damageToTake)
    {
        EntityHealth -= damageToTake;
        EntityHealth = Mathf.Clamp(EntityHealth, 0, 100);
        OnEntityDamaged?.Invoke(EntityHealth);

        if (entityHealth == 0)
        {
            Explode();
        }
    }
    private void Explode()
    {
        enemyCol.enabled = false;
        enemySpeed = 2;

        enemyAnim.SetTrigger(enemyAnim_DeathTriggerHash);
        enemyAudioSource.PlayOneShot(explosionClip);
        GameManager.Instance?.AddScore(scoreToGive);
    }
    public void Death()
    {
        OnEntityKilled?.Invoke(this);
        Destroy(gameObject);
    }
    #endregion
}