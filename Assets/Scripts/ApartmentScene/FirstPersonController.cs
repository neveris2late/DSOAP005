using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("移动设置 (Movement)")]
    public float walkSpeed = 3.5f; // 2077的室内步行速度偏慢且沉稳
    public float gravity = -9.81f;

    [Header("视角设置 (Look)")]
    public float mouseSensitivity = 2.0f;
    public Transform playerCamera;
    public float upDownRange = 85.0f; // 限制抬头和低头的最大角度，防止“折断脖子”

    private CharacterController characterController;
    private float verticalRotation;
    private Vector3 velocity;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        // 锁定并隐藏鼠标指针，这是FPS游戏的标准操作
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        // 获取鼠标输入
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 处理摄像机的上下旋转 (X轴)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

        // 处理玩家身体的左右旋转 (Y轴)
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        // 获取键盘 WASD 输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 根据玩家当前的朝向计算移动方向
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;

        // 执行平面移动
        characterController.Move(moveDirection * walkSpeed * Time.deltaTime);

        // 处理重力，让角色贴紧地面
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}