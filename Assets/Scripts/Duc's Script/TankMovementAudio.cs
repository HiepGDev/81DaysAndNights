using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TankMovementAudio : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float minimumMoveSpeed = 0.1f;

    [Header("Movement Audio")]
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField] private AudioClip movementSound;

    [Header("Tank Wheels")]
    [SerializeField] private Transform[] leftWheels;
    [SerializeField] private Transform[] rightWheels;

    [Header("Wheel Settings")]
    [SerializeField] private float wheelRotationMultiplier = 150f;

    [Tooltip("Tick nếu bánh quay ngược chiều.")]
    [SerializeField] private bool invertWheelRotation;

    // Góc quay tích lũy của bánh
    private float wheelAngle;

    // Lưu Rotation ban đầu của từng bánh
    private Vector3[] leftInitialEuler;
    private Vector3[] rightInitialEuler;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (movementAudioSource != null)
        {
            movementAudioSource.playOnAwake = false;
            movementAudioSource.loop = true;
        }

        // Lưu rotation GỐC của model
        leftInitialEuler = SaveInitialRotations(leftWheels);
        rightInitialEuler = SaveInitialRotations(rightWheels);
    }

    private void Update()
    {
        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            StopMovementAudio();
            return;
        }

        float speed = agent.velocity.magnitude;
        bool isMoving = speed > minimumMoveSpeed;

        UpdateMovementAudio(isMoving);

        if (isMoving)
            UpdateWheels(speed);
    }

    private Vector3[] SaveInitialRotations(Transform[] wheels)
    {
        if (wheels == null)
            return null;

        Vector3[] rotations = new Vector3[wheels.Length];

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null)
                rotations[i] = wheels[i].localEulerAngles;
        }

        return rotations;
    }

    private void UpdateWheels(float speed)
    {
        float direction = invertWheelRotation ? -1f : 1f;

        // Tăng DUY NHẤT góc X
        wheelAngle +=
            speed *
            wheelRotationMultiplier *
            direction *
            Time.deltaTime;

        // Không để số tăng vô hạn
        wheelAngle %= 360f;

        RotateWheels(
            leftWheels,
            leftInitialEuler,
            wheelAngle
        );

        RotateWheels(
            rightWheels,
            rightInitialEuler,
            wheelAngle
        );
    }

    private void RotateWheels(
        Transform[] wheels,
        Vector3[] initialRotations,
        float angle)
    {
        if (wheels == null || initialRotations == null)
            return;

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null)
                continue;

            Vector3 original = initialRotations[i];

            // QUAN TRỌNG:
            // X thay đổi
            // Y và Z LUÔN giữ nguyên như model ban đầu
            wheels[i].localEulerAngles = new Vector3(
                original.x + angle,
                original.y,
                original.z
            );
        }
    }

    private void UpdateMovementAudio(bool isMoving)
    {
        if (movementAudioSource == null ||
            movementSound == null)
            return;

        if (isMoving)
        {
            if (!movementAudioSource.isPlaying)
            {
                movementAudioSource.clip = movementSound;
                movementAudioSource.Play();
            }
        }
        else
        {
            StopMovementAudio();
        }
    }

    private void StopMovementAudio()
    {
        if (movementAudioSource != null &&
            movementAudioSource.isPlaying)
        {
            movementAudioSource.Stop();
        }
    }
}