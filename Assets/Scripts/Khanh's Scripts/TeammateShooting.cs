using UnityEngine;
using System.Collections;
using static TeammateAI;

[RequireComponent(typeof(TeammateDetection))]
public class TeammateShooting : MonoBehaviour
{
    public enum FireMode { Auto, SemiAuto }

    private TeammateDetection detection;
    private Animator animator;

    [Header("Weapon Stats")]    
    [SerializeField] private FireMode currentFireMode = FireMode.SemiAuto;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float autoFireRate = 0.12f;
    [SerializeField] private float semiFireRateMin = 0.3f;
    [SerializeField] private float semiFireRateMax = 0.6f;
    [SerializeField] private float fireDistance = 20.0f;

    [Header("CS:GO Style Spray Pattern")]
    [SerializeField]
    private Vector2[] sprayPattern = new Vector2[]
    {
        new Vector2(0, 0), new Vector2(0, 0.2f), new Vector2(0, 0.5f),
        new Vector2(-0.2f, 0.8f), new Vector2(-0.4f, 1.0f), new Vector2(0.2f, 1.2f)
    };
    [SerializeField] private float patternScale = 0.5f;

    [Header("Ammo Settings")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2.5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField][Range(0f, 1f)] private float shootVolume = 0.8f;

    private int currentAmmo;
    private int recoilIndex = 0;
    private float nextFireTime;
    private bool isShootingInProgress = false;
    private bool isReloading = false;
    private bool isCrouched = false;

    public bool IsOutOfAmmo => currentAmmo <= 0;
    public bool IsReloading => isReloading;

    private void Awake()
    {
        detection = GetComponent<TeammateDetection>();
        currentAmmo = magazineSize;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (isReloading || currentAmmo <= 0) return;

        if (detection == null || detection.CurrentTarget == null)
        {
            if (isShootingInProgress) EndShooting();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, detection.CurrentTarget.position);
        if (distanceToTarget > fireDistance)
        {
            if (isShootingInProgress) EndShooting();
            return;
        }

        if (!isShootingInProgress) StartShooting();

        if (Time.time >= nextFireTime)
        {
            AimAtTarget();
            Shoot();

            if (currentFireMode == FireMode.Auto)
                nextFireTime = Time.time + autoFireRate;
            else
            {
                nextFireTime = Time.time + Random.Range(semiFireRateMin, semiFireRateMax);
                recoilIndex = 0;
            }
        }
    }

    private void AimAtTarget()
    {
        if (firePoint == null || detection.CurrentTarget == null) return;

        Vector3 targetPos;
        Collider targetCollider = detection.CurrentTarget.GetComponent<Collider>();

        if (targetCollider != null)
            targetPos = targetCollider.bounds.center;
        else
            targetPos = detection.CurrentTarget.position + Vector3.up * 1.4f;

        Vector3 direction = (targetPos - firePoint.position).normalized;
        firePoint.rotation = Quaternion.LookRotation(direction);
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;
        currentAmmo--;

        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound, shootVolume);

        Vector3 shootDirection = firePoint.forward;

        if (sprayPattern != null && sprayPattern.Length > 0)
        {
            Vector2 patternOffset = sprayPattern[recoilIndex % sprayPattern.Length] * patternScale;
            shootDirection = Quaternion.AngleAxis(-patternOffset.y, firePoint.right)
                           * Quaternion.AngleAxis(patternOffset.x, firePoint.up)
                           * shootDirection;
        }

        Quaternion finalRotation = Quaternion.LookRotation(shootDirection);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, finalRotation);

        BulletDamage bulletDamage = bullet.GetComponentInChildren<BulletDamage>();
        if (bulletDamage != null) bulletDamage.isTeammateBullet = true;

        bullet.AddComponent<TeammateBulletCollision>();

        ParticleSystem ps = bullet.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ps.Play();
        }

        recoilIndex++;
        Destroy(bullet, 2.0f);

        if (currentAmmo <= 0) EndShooting();
    }

    private void StartShooting()
    {
        isShootingInProgress = true;
        recoilIndex = 0;
        nextFireTime = Time.time + 0.25f;

        // Xóa Random Crouch đi, Teammate chỉ ngồi khi nạp đạn hoặc núp Cover
        isCrouched = false;

        if (animator != null)
        {
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", true);
            animator.CrossFade("Enemy_Shooting", 0.1f);
        }
    }

    public void EndShooting()
    {
        isShootingInProgress = false;
        recoilIndex = 0;
        if (animator != null)
        {
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", false);
            animator.CrossFade("Enemy_Idle", 0.2f);
        }
    }

    public void TriggerReload()
    {
        if (!isReloading) StartCoroutine(ReloadRoutine());
    }


    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        isShootingInProgress = false;
        recoilIndex = 0;

        TeammateAI behavior = GetComponent<TeammateAI>();
        bool inCover = (behavior != null && behavior.CurrentState == TeammateAI.TeammateState.SeekingCover);

        bool shouldCrouchReload = isCrouched || inCover;

        if (animator != null)
        {
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", false);
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
            if (HasParameter("isReloading", animator)) animator.SetBool("isReloading", true);

            if (shouldCrouchReload)
            {
                animator.CrossFade("crouching_reload", 0.1f);

                float animDuration = 3.2f;
                yield return new WaitForSeconds(animDuration);

                if (isReloading && inCover)
                {
                    animator.CrossFade("Cover_Crouching", 0.2f);
                }

                float remaining = Mathf.Max(0, reloadTime - animDuration);
                if (remaining > 0) yield return new WaitForSeconds(remaining);
            }
            else
            {
                animator.CrossFade("reload_standing", 0.1f);
                yield return new WaitForSeconds(reloadTime);
            }
        }
        else
        {
            yield return new WaitForSeconds(reloadTime);
        }

        currentAmmo = magazineSize;
        isReloading = false;

        if (animator != null)
        {
            if (HasParameter("isReloading", animator)) animator.SetBool("isReloading", false);

            // Nếu KHÔNG ở trong cover thì đứng lên. Nếu ở trong cover thì tiếp tục ngồi núp.
            if (!inCover)
            {
                if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
                animator.CrossFade("Enemy_Idle", 0.1f);
            }
        }

        Debug.Log("[Teammate Weapon] Reload Complete.");
    }


    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
            if (param.name == paramName) return true;
        return false;
    }
}