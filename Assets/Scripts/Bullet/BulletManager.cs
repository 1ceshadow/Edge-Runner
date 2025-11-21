using System.Collections.Generic;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    // 静态实例，让其他脚本可以直接访问
    public static BulletManager Instance;
    
    private List<Transform> activeBullets = new List<Transform>();
    
    private void Awake()
    {
        // 确保只有一个实例存在
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景不销毁
        }
        else
        {
            Destroy(gameObject); // 如果已存在，销毁新的
        }
    }
    
    public void RegisterBullet(Transform bullet) 
    { 
        if (!activeBullets.Contains(bullet))
            activeBullets.Add(bullet); 
    }
    
    public void UnregisterBullet(Transform bullet) 
    { 
        activeBullets.Remove(bullet); 
    }
    
    //极限闪避检测
    public bool CheckBulletsInRange(Vector2 position, float radius)
    {
        foreach (Transform bullet in activeBullets)
        {
            if (Vector2.Distance(position, bullet.position) <= radius)
                return true;
        }
        return false;
    }
}