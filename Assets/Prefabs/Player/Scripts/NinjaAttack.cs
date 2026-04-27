using UnityEngine;

public class NinjaAttack : MonoBehaviour
{
    public GameObject swordTip;

    public void EnableSwordTrail()
    {
        swordTip.SetActive(true);
    }

    public void DisableSwordTrail()
    {
        swordTip.SetActive(false);
    }
}