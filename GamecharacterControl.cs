using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class GamecharacterControl : MonoBehaviour
{
    public static bool CanMove = true;

    private Animator animator;
    private Rigidbody2D rbody2D;

    private Vector2 moveInput; // 儲存輸入向量

    public float moveSpeed = 5f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rbody2D = GetComponent<Rigidbody2D>();
    }
    void OnEnable()
    {
        if (GameCharacterManager.Instance.actingCharacter == null && GameCharacterManager.Instance != null)
            GameCharacterManager.Instance.SetActing(gameObject);
    }

    void Update()
    {
        if (!CanMove || GameCharacterManager.Instance.actingCharacter != gameObject)
        {
            moveInput = Vector2.zero;
            animator.SetFloat("Speed", 0);
            rbody2D.linearVelocity = Vector2.zero;
            return;
        }
        
        // 從新 Input System 讀取輸入
        moveInput = Vector2.zero;

        // 鍵盤輸入
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y = 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y = -1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x = 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x = -1;
        }

        // 設定動畫方向
        if (Mathf.Abs(moveInput.y) > 0.01f)
        {
            animator.SetFloat("Horizontal", 0);
            animator.SetFloat("Vertical", moveInput.y);
        }
        else if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            animator.SetFloat("Horizontal", moveInput.x);
            animator.SetFloat("Vertical", 0);
        }

        animator.SetFloat("Speed", moveInput.magnitude);

        rbody2D.linearVelocity = moveInput.normalized * moveSpeed;
    }
}
