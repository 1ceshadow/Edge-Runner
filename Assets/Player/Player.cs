using UnityEngine;                          
//                                                                                          ####################################

public class Player : MonoBehaviour
{
    public Rigidbody2D rb;
    public Animator animator;

    private float xInput;
    private float yInput;
    private const float moveSpeed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }

}
