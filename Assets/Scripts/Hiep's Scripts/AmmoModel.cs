using UnityEngine;

public class AmmoModel : MonoBehaviour
{
    [SerializeField] private GameObject ammoModel; 

    //  the function the Animator will find
    public void SetRiceVisible(int visible)
    {
        if (ammoModel != null)
        {
            // If the Animator sends 1, the rice turns on. If 0, it turns off 
            ammoModel.SetActive(visible == 1);
        }
    }
}
