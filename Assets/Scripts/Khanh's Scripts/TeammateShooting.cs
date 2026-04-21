using UnityEngine;
using System.Collections; // Bắt buộc phải có thư viện này để dùng Coroutine (nạp đạn)

[RequireComponent(typeof(TeammateDetection))]
public class TeammateShooting : MonoBehaviour
{
    private TeammateDetection detection;
    private Animator animator;

    [Header("Weapon Stats")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.12f; // Tốc độ xả đạn nhanh hơn
    [SerializeField] private float fireDistance = 20.0f;

    [Header("CS:GO Style Spray Pattern")]
    [Tooltip("X = Lệch ngang, Y = Lệch dọc. Đạn sẽ bay theo thứ tự này.")]
    [SerializeField]
    private Vector2[] sprayPattern = new Vector2[]
    {
        new Vector2(0, 0),       // Viên 1: Chuẩn xác
        new Vector2(0, 0.2f),    // Viên 2: Hơi giật lên
        new Vector2(0, 0.5f),    // Viên 3
        new Vector2(-0.2f, 0.8f),// Viên 4: Lệch trái
        new Vector2(-0.4f, 1.0f),// Viên 5
        new Vector2(0.2f, 1.2f), // Viên 6: Giật mạnh sang phải
        new Vector2(0.5f, 1.3f), // Viên 7
        new Vector2(0.3f, 1.1f), // Viên 8
        new Vector2(-0.1f, 0.9f),// Viên 9
        new Vector2(-0.3f, 1.0f) // Viên 10
    };
    [SerializeField] private float patternScale = 0.5f; // Hệ số nhân độ giật

    [Header("Ammo Settings")]
    [SerializeField] private int magazineSize = 30; // Băng đạn 30 viên
    [SerializeField] private float reloadTime = 2.5f; // Thời gian thay đạn

    [Header("Crouch Settings")]
    [SerializeField] private float crouchChance = 0.5f; // 50% tỷ lệ ngồi bắn


    private int currentAmmo;
    private int recoilIndex = 0;
    private float nextFireTime;

    private bool isShootingInProgress = false;
    private bool isReloading = false;
    private bool isCrouched = false;

    private void Awake()
    {
        detection = GetComponent<TeammateDetection>();
        currentAmmo = magazineSize; // Nạp đầy đạn khi mới sinh ra
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Giữ lại các dòng check lỗi rất tốt của bạn
        if (bulletPrefab == null)
            Debug.LogError($"[TeammateShooting] '{gameObject.name}': bulletPrefab chưa được gán!", this);
        if (firePoint == null)
            Debug.LogError($"[TeammateShooting] '{gameObject.name}': firePoint chưa được gán!", this);
        if (detection == null)
            Debug.LogError($"[TeammateShooting] '{gameObject.name}': Không tìm thấy TeammateDetection!", this);
    }

    private void Update()
    {
        // Đang nạp đạn thì không làm gì cả
        if (isReloading) return;

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

        // Kích hoạt trạng thái bắn
        if (!isShootingInProgress) StartShooting();


        // Xả đạn
        if (Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                AimAtTarget();
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
            else
            {
                StartCoroutine(Reload()); // Hết đạn thì nạp
            }
        }
    }

    //private void LateUpdate()
    //{
    //    if (isReloading) return;
    //    if (detection == null || detection.CurrentTarget == null) return;
    //    if (!isShootingInProgress) return;

    //    AimAtTarget();
    //}

    private void AimAtTarget()
    {
        if (firePoint == null) return;

        // Cúi thì bắn thấp, đứng thì bắn cao
        float aimHeight = isCrouched ? 1f : 1.4f;
        Vector3 targetPos = detection.CurrentTarget.position + Vector3.up * aimHeight;
        firePoint.LookAt(targetPos);
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        currentAmmo--; // Trừ đạn

        // 1. Tính toán độ giật dựa trên mảng sprayPattern
        Vector2 patternOffset = Vector2.zero;
        if (sprayPattern != null && sprayPattern.Length > 0)
        {
            patternOffset = sprayPattern[recoilIndex % sprayPattern.Length] * patternScale;
        }

        // 2. Bẻ cong hướng nòng súng (Tạo độ giật giả)
        Quaternion recoilRotation = Quaternion.Euler(-patternOffset.y, patternOffset.x, 0);
        Quaternion finalRotation = firePoint.rotation * recoilRotation;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, finalRotation);

        BulletDamage bulletDamage = bullet.GetComponentInChildren<BulletDamage>();
        if (bulletDamage != null) bulletDamage.isTeammateBullet = true;

        ParticleSystem ps = bullet.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ps.Play();
        }

        recoilIndex++; // Tăng chỉ số giật cho viên tiếp theo
        Destroy(bullet, 2.0f);
    }

    private void StartShooting()
    {
        isShootingInProgress = true;
        recoilIndex = 0; // Reset độ giật súng về 0 khi bắt đầu loạt đạn mới
        isCrouched = Random.value > (1 - crouchChance);

        if (animator != null)
        {
            animator.SetBool("isShooting", false);
            animator.SetBool("isCrouching", false);

            if (isCrouched)
            {
                animator.SetBool("isCrouching", true);
                animator.CrossFade("Enemy_CrouchShooting", 0.1f);
            }
            else
            {
                animator.SetBool("isShooting", true);
                animator.CrossFade("Enemy_Shooting", 0.1f);
            }
        }
    }

    private void EndShooting()
    {
        isShootingInProgress = false;
        recoilIndex = 0; // Reset độ giật

        if (animator != null)
        {
            animator.SetBool("isShooting", false);
            animator.SetBool("isCrouching", false);
            animator.CrossFade("Enemy_Idle", 0.2f); // Chuyển về Idle mượt mà
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        recoilIndex = 0; // Thay đạn xong súng hết giật

        isShootingInProgress = false;

        if (animator != null)
        {
            animator.SetBool("isShooting", false);
            animator.SetBool("isCrouching", false);
            animator.SetBool("isReloading", true);
            animator.CrossFade("Enemy_Reload", 0.1f);
        }

        yield return new WaitForSeconds(reloadTime); // Chờ 2.5 giây

        currentAmmo = magazineSize; // Nạp đầy
        isReloading = false;

        if (animator != null) animator.SetBool("isReloading", false);
        animator.CrossFade("Enemy_Idle", 0.1f);
    }
}