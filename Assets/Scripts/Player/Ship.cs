﻿using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GalaxyShooter.Interfaces;

public class Ship : MonoBehaviour, IDamageable
{
    #region Variables
    #region Components
    [Header("Components")]
    [SerializeField] private Collider2D     shipCollider;
    [SerializeField] private PlayerInput    shipInput;
    [SerializeField] private Animator       shipAnim;
    [SerializeField] private AudioSource    shipAudioSource;

    [Header("Child Components")]
    [SerializeField] private Animator leftEngineAnim;
    [SerializeField] private Animator rightEngineAnim;
    [SerializeField] private Animator shieldAnim;

    [SerializeField] private LaserBeam laserBeam;
    #endregion

    #region Animator Strings
    [Header("Animator References")]
    // SHIP
    [SerializeField] private string shipAnim_Input;
    private int shipAnim_InputHash => Animator.StringToHash(shipAnim_Input);

    [SerializeField] private string shipAnim_Invincibility;
    private int shipAnim_InvincibilityHash => Animator.StringToHash(shipAnim_Invincibility);

    // ENGINES
    [SerializeField] private string engineAnim_Hurt;
    private int engineAnim_HurtHash => Animator.StringToHash(engineAnim_Hurt);

    // SHIELD
    [SerializeField] private string shieldAnim_Health;
    private int shieldAnim_HealthHash => Animator.StringToHash(shieldAnim_Health);
    #endregion

    #region Input Values
    [Header("Inputs")]
    private Vector2 moveInput;
    private bool doShoot;
    private bool thrustPressed;
    #endregion

    #region Ship Health
    [Header("Ship Health")]
    [SerializeField] private int entityHealth;
    public int EntityHealth
    {
        get { return entityHealth; }
        set { entityHealth = Mathf.Clamp(value, 0, EntityMaxHealth); }
    }

    [SerializeField] private int entityMaxHealth = 3;
    public int EntityMaxHealth
    {
        get { return entityMaxHealth; }
        set { entityMaxHealth = Mathf.Clamp(value, 1, int.MaxValue); }
    }


    [Header("Ship Invincibility")]
    private WaitForSeconds invincibilityDuration = new WaitForSeconds(2f);
    private Coroutine invincibilityCoroutine;
    private bool invincible => invincibilityCoroutine != null;


    [Header("Ship Explosion")]
    [SerializeField] private GameObject explosion;
    #endregion

    #region Ship Move
    [Header("Ship Move")]
    [SerializeField] private float shipSpeed = 8f;

    [Header("Ship Thrusters")]
    [SerializeField] private float shipThrusterSpeed = 10f;
    [SerializeField] [Range(0f, 1f)] private float shipThrusterFuel = 1f;
    [SerializeField] [Range(0f, 1f)] private float shipThrusterMinumumFuelAfterExhaust = 1f;
    private bool usingThrusters = false;

    [Space(8)]

    [SerializeField] [Range(0f, 1f)] private float shipThrusterWasteSpeed = .2f;
    [SerializeField] [Range(0f, 1f)] private float shipThrusterRecoverySpeed = .4f;
    [SerializeField] [Range(0f, 10f)] private float shipThrusterRecoveryCooldown = .4f;
    #endregion

    #region Ship Shoot
    [Header("Ship Shoot")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private Vector3 laserSpawnOffset;
    [SerializeField] private float fireRate = .2f;
    private float fireRateTimer;

    [Space(8)]

    [SerializeField] private int ammoCount;
    [SerializeField] private int maxAmmo;

    [Space(8)]

    [SerializeField] [Range(0, 30)] private int shipProjectileLayer;
    #endregion

    #region PowerUps
    [Header("Abilities")]
    [SerializeField] private int shootAbilityAmmoRefill = 10;


    [Header("Triple Shoot")]
    [SerializeField] private GameObject tripleShotPrefab;

    private Coroutine tripleShotCoroutine;
    private WaitForSeconds tripleShotDuration = new WaitForSeconds(5f);
    private bool tripleShotActive => tripleShotCoroutine != null;


    [Header("Heat Seek Shoot")]
    [SerializeField] private GameObject heatSeekShotPrefab;

    private Coroutine heatSeekShotCoroutine;
    private WaitForSeconds heatSeekDuration = new WaitForSeconds(5f);
    private bool heatSeekShotActive => heatSeekShotCoroutine != null;


    [Header("Extra Speed")]
    [SerializeField] [Range(1f, 3f)] private float extraSpeedMultiplier;

    private Coroutine extraSpeedCoroutine;
    private WaitForSeconds extraSpeedDuration = new WaitForSeconds(8f);
    private bool extraSpeedActive => extraSpeedCoroutine != null;


    [Header("Beam")]
    [SerializeField] private bool beamReadyToUse;


    [Header("Shield")]
    private int shieldHealth = 0;
    private int shieldMaxHealth = 3;
    private bool shieldActive;
    #endregion

    #region Audio
    [Header("Audio")]
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip noAmmoClip;
    [SerializeField] private AudioClip explosionClip;
    #endregion

    #region Events
    // Events
    public event System.Action<int> OnShoot;
    public event System.Action<int> OnAmmoRefill;
    public event System.Action<float> OnThrusterCooldownChange;
    public event System.Action<float> OnThrusterFuelChange;
    public event System.Action<int> OnEntityHealed;
    public event System.Action<int> OnEntityDamaged;
    public event System.Action<IDamageable> OnEntityKilled;
    #endregion
    #endregion


    #region MonoBehaviour Methods
    private void Awake()
    {
        // Set Entity Health to Max Health
        entityHealth = entityMaxHealth;

        // Refill ammo
        ammoCount = maxAmmo;
    }

    private void Start()
    {
        // Start Thruster Coroutine
        StartCoroutine(ThrusterBehaviour());
    }

    // Update is called once per frame
    private void Update()
    {
        Move();
        Shoot();
    }
    #endregion

    #region Custom Methods
    #region Input
    public void UpdateMoveInput(InputAction.CallbackContext input)
    {
        // Get Player Input
        moveInput = input.ReadValue<Vector2>();

        // Update Ship Move Animations
        shipAnim.SetFloat(shipAnim_InputHash, moveInput.x);
    }
    public void UpdateShootInput(InputAction.CallbackContext input)
    {
        // Set Shoot input value
        doShoot = input.performed;

        // If the Input has been pressed, and there's no more ammo, play No Ammo Sound
        if (input.performed && ammoCount == 0 && !laserBeam.IsBeamCoroutineRunning() && !beamReadyToUse)
        {
            shipAudioSource.PlayOneShot(noAmmoClip);
        }
    }
    public void UpdateThrusterInput(InputAction.CallbackContext input)
    {
        // Set Thrust Input value
        thrustPressed = input.ReadValueAsButton();
    }
    #endregion

    #region Movement
    private void Move()
    {
        // Move Ship - Uses Normalized Input and Selects a Speed
        transform.Translate(moveInput.normalized * SpeedSelection() * Time.deltaTime);

        MoveHorizontalWrap();
        MoveVerticalClamp();
    }

    private float SpeedSelection()
    {
        // If Player is using the Thrusters, apply Thruster speed. Else, keep normal ship speed
        float selectedSpeed = usingThrusters ? shipThrusterSpeed : shipSpeed;

        // If Extra Speed Power-up is active, apply extra speed. Else, keep selected Speed
        selectedSpeed *= extraSpeedActive ? extraSpeedMultiplier : 1f;

        return selectedSpeed;
    }
    private void MoveHorizontalWrap()
    {
        // Wrap ship around X axis
        if (Mathf.Abs(transform.position.x) > SpaceShooterData.WrapX)
        {
            transform.position = new Vector3(SpaceShooterData.WrapX * Mathf.Sign(transform.position.x) * -1, transform.position.y);
        }
    }
    private void MoveVerticalClamp()
    {
        // Clamp Y position so the player doesn't leave the screen
        transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, SpaceShooterData.PlayerBoundLimitsY.x, SpaceShooterData.PlayerBoundLimitsY.y), transform.position.z);
    }

    private IEnumerator ThrusterBehaviour()
    {
        bool successfulCooldown = false;

        while (true)
        {
            // If the Thrust Action input is pressed, has fuel, and the move input magnitude is higher than 0...
            if (thrustPressed && shipThrusterFuel > 0f && moveInput.magnitude > 0)
            {
                // Drain Thruster Capacity
                shipThrusterFuel = Mathf.Clamp(shipThrusterFuel - shipThrusterWasteSpeed * Time.deltaTime, 0f, 1f);

                // Trigger Thruster Fuel Change Event
                OnThrusterFuelChange?.Invoke(shipThrusterFuel);

                // The player is using the thrusters
                usingThrusters = true;
            }
            else
            {
                // The ship is not using the thrusters
                usingThrusters = false;

                // Else, if the ship used fuel... 
                if (shipThrusterFuel < 1f)
                {
                    successfulCooldown = true;

                    // Wait for the recovery cooldown
                    for (float i = shipThrusterRecoveryCooldown; i >= 0f; i -= Time.deltaTime)
                    {
                        // If the player wants to interrupt the thruster cooldown to use them, abort cooldown
                        if (thrustPressed && shipThrusterFuel > 0f && moveInput.magnitude > 0)
                        {
                            successfulCooldown = false;
                            OnThrusterCooldownChange?.Invoke(0f);
                            break;
                        }

                        // Trigger Thruster Cooldown Change Event
                        OnThrusterCooldownChange?.Invoke(Mathf.InverseLerp(shipThrusterRecoveryCooldown, 0f, i));

                        yield return null;
                    }

                    // No longer on cooldown
                    OnThrusterCooldownChange?.Invoke(0f);

                    // If the cooldown was successful...
                    if (successfulCooldown)
                    {
                        // While the ship is not at full fuel capacity, and while the player is not trying to use the thrusters with a minimum amount of fuel....
                        while (shipThrusterFuel < 1f && !(thrustPressed && shipThrusterFuel >= shipThrusterMinumumFuelAfterExhaust && moveInput.magnitude > 0))
                        {
                            // Refill fuel
                            shipThrusterFuel = Mathf.Clamp(shipThrusterFuel + shipThrusterRecoverySpeed * Time.deltaTime, 0f, 1f);

                            // Trigger Thruster Fuel Change Event
                            OnThrusterFuelChange?.Invoke(shipThrusterFuel);

                            yield return null;
                        }
                    }
                }
            }

            yield return null;
        }
    }
    #endregion

    #region Shoot
    private void Shoot()
    {
        // If Shoot input is performed
        if (doShoot)
        {
            if (beamReadyToUse)
            {
                FireLaserBeam();
                return;
            }

            // If the Ship has ammo and Time.time is higher than the Fire Rate Timer
            if (ammoCount != 0 && Time.time >= fireRateTimer && !laserBeam.IsBeamCoroutineRunning())
            {
                // Subtract laser
                ammoCount--;

                // Update timer
                fireRateTimer = Time.time + fireRate;

                // Choose between Triple, Heat Seek or Single laser
                GameObject laserType = laserPrefab;
                laserType = tripleShotActive ? tripleShotPrefab : laserType;
                laserType = heatSeekShotActive ? heatSeekShotPrefab : laserType;

                // Ignore own lasers
                Transform[] lasersToIgnore = Instantiate(laserType, transform.position + laserSpawnOffset, Quaternion.identity).GetComponentsInChildren<Transform>();
                for (int i = 0; i < lasersToIgnore.Length; i++)
                {
                    lasersToIgnore[i].gameObject.layer = shipProjectileLayer;
                }

                // Invoke event
                OnShoot?.Invoke(ammoCount);

                // Play Shoot soundclip
                AudioManager.Instance?.PlayOneShotClip(shootClip);
            }
        }
    }

    public int GetAmmoCount()
    {
        return ammoCount;
    }
    public int GetMaxAmmoCount()
    {
        return maxAmmo;
    }

    private void RefillAmmo()
    {
        ammoCount = maxAmmo;
        OnAmmoRefill?.Invoke(ammoCount);
    }
    private void RefillAmmo(int ammoToRefill)
    {
        ammoCount = Mathf.Clamp(ammoCount + ammoToRefill, 0, maxAmmo);
        OnAmmoRefill?.Invoke(ammoCount);
    }
    #endregion

    #region Entity Health
    private void HealShip(int amountToHeal)
    {
        // Clamp Heal Amount (No negative values or exceeding max health)
        amountToHeal = Mathf.Clamp(amountToHeal, 0, EntityMaxHealth);

        // Clamp Health
        EntityHealth = Mathf.Clamp(EntityHealth + amountToHeal, 0, EntityMaxHealth);

        // Update Engine Animators
        leftEngineAnim.SetBool(engineAnim_Hurt, EntityHealth < 3);
        rightEngineAnim.SetBool(engineAnim_Hurt, EntityHealth < 2);

        // Trigger On Heal Event, passing in the Ship's health
        OnEntityHealed?.Invoke(EntityHealth);
    }
    public void TakeDamage(int damageToTake)
    {
        // Clamp Heal Amount (No negative values or exceeding max health)
        damageToTake = Mathf.Clamp(damageToTake, 0, EntityMaxHealth);

        // If the Ship is in Invincibility state, stop executing this method
        if (invincible) return;

        // If shield is active, make the shield take the damage instead and stop executing this method
        if (shieldActive)
        {
            ShieldTakeDamage(damageToTake);
            return;
        }

        // Take damage and clamp Health
        EntityHealth = Mathf.Clamp(EntityHealth - damageToTake, 0, EntityMaxHealth);

        if (EntityHealth == 0)
        {
            // Camera Shake
            CameraShake.Instance?.AddStress(1f);

            // Destroy Ship
            Death();
        }
        else
        {
            // Activate Invincibility state
            ActivateInvincibility();

            // Update Engine Animators
            leftEngineAnim.SetBool(engineAnim_Hurt, EntityHealth < 3);
            rightEngineAnim.SetBool(engineAnim_Hurt, EntityHealth < 2);

            // Play Explosion Sound
            shipAudioSource.PlayOneShot(explosionClip);

            // Camera Shake
            switch (EntityHealth)
            {
                case 2:
                    CameraShake.Instance?.AddStress(.5f);
                    break;
                case 1:
                    CameraShake.Instance?.AddStress(.75f);
                    break;
            }          
        }

        // This event is triggered when the Ship gets damaged. Needs to pass in the Ship Health's value
        OnEntityDamaged?.Invoke(EntityHealth);
    }
    public void Death()
    {
        OnEntityKilled?.Invoke(this);

        // Instantiate Explosion and Destroy this GameObject
        Instantiate(explosion, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void ActivateInvincibility()
    {
        // If the Invincibility Coroutine is running, stop it
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
        }

        // Start Invincibility Coroutine
        invincibilityCoroutine = StartCoroutine(InvincibilityDuration());
    }
    private void StopInvincibility()
    {
        // If the Invincibility coroutine is running, stop it, clear the Coroutine variable and enable collisions
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
            invincibilityCoroutine = null;

            shipCollider.enabled = true;
        }
    }
    private IEnumerator InvincibilityDuration()
    {
        // Start Invincibility Animation
        shipAnim.SetTrigger(shipAnim_InvincibilityHash);

        // Disable Collisions
        shipCollider.enabled = false;

        yield return invincibilityDuration;

        // Enable Collisions
        shipCollider.enabled = true;

        // Clear Coroutine variable
        invincibilityCoroutine = null;
    }
    #endregion

    #region Shield
    private void ActivateShield()
    {
        // Activate Shield and Set the health to Max Shield Health
        shieldActive = true;
        shieldHealth = shieldMaxHealth;

        // Set Active Shield GameObject, and update the Shield Animator
        shieldAnim.gameObject.SetActive(shieldActive);
        shieldAnim.SetInteger(shieldAnim_HealthHash, shieldHealth);
    }
    private void StopShield()
    {
        // If the Shield is active
        if (shieldActive)
        {
            // Deactivate shield, and set the health to 0
            shieldActive = false;
            shieldHealth = 0;

            // Update Shield Animator, and Deactivate Shield GameObject
            shieldAnim.SetInteger(shieldAnim_HealthHash, shieldHealth);
            shieldAnim.gameObject.SetActive(shieldActive);
        }
    }

    private void ShieldTakeDamage(int damageToTake)
    {
        // Take Shield Damage, and clamp the value
        shieldHealth = Mathf.Clamp(shieldHealth - damageToTake, 0, shieldMaxHealth);

        // Update Shield Animator
        shieldAnim.SetInteger(shieldAnim_HealthHash, shieldHealth);

        // If the Shield is broken, stop Shield effects
        if (shieldHealth == 0)
        {
            StopPowerUp(PowerUp.Type.Shield);
        }

        // Camera Shake
        switch (damageToTake)
        {
            case 0:
                break;

            case 1:
                CameraShake.Instance?.AddStress(.5f);
                break;
            case 2:
                CameraShake.Instance?.AddStress(.75f);
                break;

            default:
                CameraShake.Instance?.AddStress(1f);
                break;
        }

        ActivateInvincibility();
    }
    #endregion

    #region Laser Beam
    private void PrepareBeam()
    {
        if (laserBeam.GetBeamState() == LaserBeam.BeamState.Inactive)
        {
            beamReadyToUse = true;
        }
        else
        {
            FireLaserBeam();
        }
    }

    private void FireLaserBeam()
    {
        beamReadyToUse = false;
        laserBeam.StartBeam();
    }
    #endregion

    #region PowerUps
    /// <summary>
    /// Triggers a ship ability.
    /// </summary>
    /// <param name="powerup">Ability to Trigger.</param>
    public void ActivatePowerUp(PowerUp.Type powerup)
    {
        // Instead of having multiple methods, start every Coroutine here
        switch (powerup)
        {
            case PowerUp.Type.TripleShot:
                if (heatSeekShotCoroutine != null) StopPowerUp(PowerUp.Type.HeatSeek);
                if (tripleShotCoroutine != null) StopPowerUp(PowerUp.Type.TripleShot);
                beamReadyToUse = false;

                tripleShotCoroutine = StartCoroutine(AbilityDuration(() => StopPowerUp(PowerUp.Type.TripleShot), tripleShotDuration));
                RefillAmmo(shootAbilityAmmoRefill);
                break;

            case PowerUp.Type.HeatSeek:
                if (tripleShotCoroutine != null) StopPowerUp(PowerUp.Type.TripleShot);
                if (heatSeekShotCoroutine != null) StopPowerUp(PowerUp.Type.HeatSeek);
                beamReadyToUse = false;

                heatSeekShotCoroutine = StartCoroutine(AbilityDuration(() => StopPowerUp(PowerUp.Type.HeatSeek), heatSeekDuration));
                RefillAmmo(shootAbilityAmmoRefill);
                break;

            case PowerUp.Type.Speed:
                if (extraSpeedCoroutine != null) StopPowerUp(PowerUp.Type.TripleShot);

                extraSpeedCoroutine = StartCoroutine(AbilityDuration(() => StopPowerUp(PowerUp.Type.Speed), extraSpeedDuration));
                break;

            case PowerUp.Type.Shield:
                ActivateShield();
                break;

            case PowerUp.Type.Life:
                HealShip(1);
                break;

            case PowerUp.Type.Ammo:
                RefillAmmo();
                break;

            case PowerUp.Type.Beam:
                PrepareBeam();
                break;
        }
    }

    /// <summary>
    /// Interrupts a ship ability, if it's active.
    /// </summary>
    /// <param name="powerup">Ability to Stop.</param>
    private void StopPowerUp(PowerUp.Type powerup)
    {
        switch (powerup)
        {
            case PowerUp.Type.TripleShot:
                if (tripleShotCoroutine != null)
                {
                    StopCoroutine(tripleShotCoroutine);
                    tripleShotCoroutine = null;
                }
                break;

            case PowerUp.Type.HeatSeek:
                if (heatSeekShotCoroutine != null)
                {
                    StopCoroutine(heatSeekShotCoroutine);
                    heatSeekShotCoroutine = null;
                }
                break;

            case PowerUp.Type.Speed:
                if (extraSpeedCoroutine != null)
                {
                    StopCoroutine(extraSpeedCoroutine);
                    extraSpeedCoroutine = null;
                }
                break;

            case PowerUp.Type.Shield:
                StopShield();
                break;
        }
    }

    private IEnumerator AbilityDuration(System.Action OnEndCallback, WaitForSeconds abilityDuration)
    {
        yield return abilityDuration;
        OnEndCallback?.Invoke();
    }
    #endregion
    #endregion
}