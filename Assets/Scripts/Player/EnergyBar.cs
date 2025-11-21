using System.Collections;
using UnityEngine;
using UnityEngine.UI;



public class EnergyBar : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    public Image energyBarImg;
    public Image RewardEffecctImg;
    private Coroutine rewardCoroutine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RewardEffecctImg.fillAmount = 0f;
        // Transform Player = this.transform.parent;
        // playerMovement = Player.GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        energyBarImg.fillAmount = playerMovement.currentEnergy / playerMovement.maxEnergy;
        if (playerMovement.isRewarded)
        {
            playerMovement.isRewarded = false;
            if (rewardCoroutine != null)
            {
                StopCoroutine(rewardCoroutine);
            }
            rewardCoroutine = StartCoroutine(ShowRewardEffect());
        }
    }
    private IEnumerator ShowRewardEffect()
    {
        //透明度-显示
        RewardEffecctImg.fillAmount = 1f;
        yield return new WaitForSeconds(0.4f);
        //透明度-隐藏
        RewardEffecctImg.fillAmount = 0f;
    }
}
