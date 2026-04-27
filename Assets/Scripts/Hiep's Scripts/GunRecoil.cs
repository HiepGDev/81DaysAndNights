using UnityEngine;

public class GunRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    [SerializeField] private float recoilX = 2f;       // Vertical kick
    [SerializeField] private float recoilY = 1f;       // Horizontal sway
    [SerializeField] private float snappiness = 15f;   // How fast recoil applies
    [SerializeField] private float returnSpeed = 6f;   // How fast it settles

    private Vector2 currentRecoil = Vector2.zero; // [x = pitch, y = yaw]
    private Vector2 targetRecoil = Vector2.zero;

    void LateUpdate()
    {
        // Smoothly approach the target recoil
        currentRecoil = Vector2.Lerp(currentRecoil, targetRecoil, snappiness * Time.deltaTime);

        // Apply the recoil to camera rotation
        transform.localRotation = Quaternion.Euler(currentRecoil.x, currentRecoil.y, 0f);

        // Fade the target recoil back to zero
        targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero, returnSpeed * Time.deltaTime);
    }

    public void FireRecoil()
    {
        float x = Random.Range(recoilX * 0.8f, recoilX);
        float y = Random.Range(-recoilY, recoilY);

        targetRecoil += new Vector2(-x, y);
    }
}
