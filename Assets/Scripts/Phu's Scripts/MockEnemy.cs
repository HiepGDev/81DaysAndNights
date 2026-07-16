using UnityEngine;
using UnityEngine.AI;
using PurrNet;

namespace PhuScene
{
    public enum EnemyType
    {
        Basic,
        Elite,
        Boss
    }

    public class MockEnemy : NetworkBehaviour
    {
        [Header("Stats")]
        [SerializeField] private EnemyType enemyType = EnemyType.Basic;
        public EnemyType Type => enemyType;
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private SyncVar<float> currentHealth = new(50f);
        [SerializeField] private float speed = 3.5f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackCooldown = 1.5f;

        [Header("Target Tracking")]
        [SerializeField] private Transform targetPlayer;
        private float lastAttackTime = 0f;

        // Navigation
        private NavMeshAgent agent;
        private bool hasNavMeshAgent = false;

        // UI text mesh
        private TextMesh floatingText;

        public void SetupMock(EnemyType type, Transform player)
        {
            this.enemyType = type;
            this.targetPlayer = player;

            switch (type)
            {
                case EnemyType.Basic:
                    maxHealth = 40f;
                    speed = 3.2f;
                    damage = 8f;
                    SetColor(Color.green);
                    break;
                case EnemyType.Elite:
                    maxHealth = 90f;
                    speed = 4.0f;
                    damage = 18f;
                    SetColor(new Color(1f, 0.5f, 0f)); // Orange
                    break;
                case EnemyType.Boss:
                    maxHealth = 250f;
                    speed = 2.5f;
                    damage = 40f;
                    transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                    SetColor(Color.red);
                    break;
            }

            currentHealth.value = maxHealth;
            CreateFloatingText();
        }

        public void ScaleStats(float healthMult, float damageMult, float speedMult)
        {
            maxHealth *= healthMult;
            currentHealth.value = maxHealth;
            damage *= damageMult;
            speed *= speedMult;

            if (agent != null)
            {
                agent.speed = speed;
            }

            UpdateFloatingText();
        }

        private void Start()
        {
            if (isSpawned && !isServer)
            {
                return;
            }

            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }

            if (agent != null)
            {
                agent.speed = speed;
                agent.acceleration = 12f;
                agent.stoppingDistance = attackRange - 0.2f;
                hasNavMeshAgent = true;
            }

            if (!isSpawned)
            {
                currentHealth.onChanged += OnHealthChanged;
            }

            UpdateFloatingText();
        }

        protected override void OnSpawned()
        {
            currentHealth.onChanged += OnHealthChanged;
            UpdateFloatingText();
        }

        protected override void OnDespawned()
        {
            currentHealth.onChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float newHealth)
        {
            UpdateFloatingText();
            StartCoroutine(FlashWhiteOnDamage());

            if (newHealth <= 0f)
            {
                if (!isSpawned || isServer)
                {
                    Die();
                }
            }
        }

        private void TargetNearestPlayer()
        {
            if (isSpawned)
            {
                var players = FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None);
                float minDistance = float.MaxValue;
                Transform nearestPlayer = null;
                foreach (var p in players)
                {
                    if (p.IsDead) continue;
                    float dist = Vector3.Distance(transform.position, p.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestPlayer = p.transform;
                    }
                }
                targetPlayer = nearestPlayer;
            }
        }

        private void Update()
        {
            if (isSpawned && !isServer)
            {
                if (floatingText != null && Camera.main != null)
                {
                    floatingText.transform.rotation = Quaternion.LookRotation(floatingText.transform.position - Camera.main.transform.position);
                }
                return;
            }

            TargetNearestPlayer();

            if (targetPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

                if (hasNavMeshAgent && agent.isOnNavMesh && agent.isActiveAndEnabled)
                {
                    agent.SetDestination(targetPlayer.position);
                }
                else
                {
                    Vector3 direction = (targetPlayer.position - transform.position).normalized;
                    direction.y = 0;
                    transform.position += direction * speed * Time.deltaTime;
                    
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
                    }
                }

                if (distanceToPlayer <= attackRange)
                {
                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        AttackPlayer();
                        lastAttackTime = Time.time;
                    }
                }
            }

            if (floatingText != null && Camera.main != null)
            {
                floatingText.transform.rotation = Quaternion.LookRotation(floatingText.transform.position - Camera.main.transform.position);
            }
        }

        private void AttackPlayer()
        {
            if (targetPlayer == null) return;

            var netHealth = targetPlayer.GetComponent<NetworkPlayerHealth>();
            if (netHealth != null)
            {
                Debug.Log($"[MockEnemy] {gameObject.name} attacks networked player for {damage} damage!");
                netHealth.TakeDamage(damage);
                return;
            }

            PlayerHealth playerHealth = targetPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"[MockEnemy] {gameObject.name} attacks local player for {damage} damage!");
                playerHealth.TakeDamage(damage);
            }
        }

        public void TakeDamage(float damageAmount)
        {
            if (isSpawned && !isServer) return;

            currentHealth.value -= damageAmount;
            currentHealth.value = Mathf.Max(0f, currentHealth.value);
            Debug.Log($"[MockEnemy] {gameObject.name} took {damageAmount} damage! Current HP: {currentHealth.value}/{maxHealth}");
        }

        private System.Collections.IEnumerator FlashWhiteOnDamage()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.material.color;
                renderer.material.color = Color.white;
                yield return new WaitForSeconds(0.12f);
                if (renderer != null)
                {
                    renderer.material.color = originalColor;
                }
            }
        }

        private void Die()
        {
            Debug.Log($"[MockEnemy] {gameObject.name} died!");
            
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.ReportMockEnemyDeath(gameObject);
            }

            Destroy(gameObject);
        }

        private void SetColor(Color color)
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                r.material.color = color;
            }
        }

        private void CreateFloatingText()
        {
            GameObject textObj = new GameObject("FloatingText");
            textObj.transform.SetParent(this.transform);
            textObj.transform.localPosition = new Vector3(0, 1.3f, 0);

            floatingText = textObj.AddComponent<TextMesh>();
            floatingText.characterSize = 0.12f;
            floatingText.fontSize = 24;
            floatingText.anchor = TextAnchor.MiddleCenter;
            floatingText.alignment = TextAlignment.Center;
            floatingText.color = Color.white;
        }

        private void UpdateFloatingText()
        {
            if (floatingText != null)
            {
                string nameColor = enemyType == EnemyType.Boss ? "red" : (enemyType == EnemyType.Elite ? "orange" : "green");
                floatingText.text = $"[{enemyType}]\nHP: {Mathf.CeilToInt(currentHealth.value)}/{Mathf.CeilToInt(maxHealth)}";
            }
        }

        public void TakeDamage(int damageAmount)
        {
            TakeDamage((float)damageAmount);
        }
    }
}
