using UnityEngine;
using UnityEngine.UI;



public class EnergyBar : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    public Image energyBarImg;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Transform Player = this.transform.parent;
        // playerMovement = Player.GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        energyBarImg.fillAmount = playerMovement.currentEnergy / playerMovement.maxEnergy;
    }
}
