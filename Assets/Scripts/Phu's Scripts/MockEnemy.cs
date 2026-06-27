using UnityEngine;
using UnityEngine.AI;

namespace PhuScene
{
    public enum EnemyType
    {
        Basic,
        Elite,
        Boss
    }

    public class MockEnemy : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private EnemyType enemyType = EnemyType.Basic;
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float currentHealth;
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

            // Define base stats based on type
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
                    // Make Boss visually larger
                    transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                    SetColor(Color.red);
                    break;
            }

            currentHealth = maxHealth;

            // Setup floating status text
            CreateFloatingText();
        }

        public void ScaleStats(float healthMult, float damageMult, float speedMult)
        {
            maxHealth *= healthMult;
            currentHealth = maxHealth;
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
            // Setup NavMeshAgent dynamically
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

            UpdateFloatingText();
        }

        private void Update()
        {
            // Move toward player
            if (targetPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

                if (hasNavMeshAgent && agent.isOnNavMesh && agent.isActiveAndEnabled)
                {
                    agent.SetDestination(targetPlayer.position);
                }
                else
                {
                    // Fallback move directly
                    Vector3 direction = (targetPlayer.position - transform.position).normalized;
                    direction.y = 0; // Keep on flat ground
                    transform.position += direction * speed * Time.deltaTime;
                    
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
                    }
                }

                // Attack logic
                if (distanceToPlayer <= attackRange)
                {
                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        AttackPlayer();
                        lastAttackTime = Time.time;
                    }
                }
            }

            // Keep floating text facing camera
            if (floatingText != null && Camera.main != null)
            {
                floatingText.transform.rotation = Quaternion.LookRotation(floatingText.transform.position - Camera.main.transform.position);
            }
        }

        private void AttackPlayer()
        {
            if (targetPlayer == null) return;

            PlayerHealth playerHealth = targetPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"[MockEnemy] {gameObject.name} attacks player for {damage} damage!");
                playerHealth.TakeDamage(damage);
            }
        }

        public void TakeDamage(float damageAmount)
        {
            currentHealth -= damageAmount;
            currentHealth = Mathf.Max(0f, currentHealth);
            Debug.Log($"[MockEnemy] {gameObject.name} took {damageAmount} damage! Current HP: {currentHealth}/{maxHealth}");

            UpdateFloatingText();

            // Simple visual damage feedback: momentarily change color to white
            StartCoroutine(FlashWhiteOnDamage());

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private System.Collections.IEnumerator FlashWhiteOnDamage()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.material.color;
                renderer.material.color = Color.white;
                yield return new WaitForSeconds(0.12f);
                if (renderer != null) // check in case destroyed
                {
                    renderer.material.color = originalColor;
                }
            }
        }

        private void Die()
        {
            Debug.Log($"[MockEnemy] {gameObject.name} died!");
            
            // Notify WaveManager
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.ReportMockEnemyDeath(gameObject);
            }

            // Play a small particle-like effect or sound if desired, then destroy
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
            textObj.transform.localPosition = new Vector3(0, 1.3f, 0); // Above the capsule

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
                floatingText.text = $"[{enemyType}]\nHP: {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
            }
        }

        // Support standard damage messages (e.g. if hit by bullet scripts that call TakeDamage with int)
        public void TakeDamage(int damageAmount)
        {
            TakeDamage((float)damageAmount);
        }
    }
}
