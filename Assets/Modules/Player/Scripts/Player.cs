using UnityEngine;                          

public class Player : MonoBehaviour, IPlayerService
{
    public Rigidbody2D rb;
    public Animator animator;

    private float xInput;
    private float yInput;
    private const float moveSpeed = 5f;
    
    // IPlayerService 实现（显式接口实现）
    Transform IPlayerService.Transform => transform;
    GameObject IPlayerService.GameObject => gameObject;
    
    T IPlayerService.GetComponent<T>()
    {
        return GetComponent<T>();
    }
    
    bool IPlayerService.TryGetComponent<T>(out T component)
    {
        return TryGetComponent(out component);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        
        // VContainer 会自动注册此组件
        Debug.Log("✓ Player 初始化完成（由 VContainer 管理）");
    }

    // Update is called once per frame
    void Update()
    {

    }

}
