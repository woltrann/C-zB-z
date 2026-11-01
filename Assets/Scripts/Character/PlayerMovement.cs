using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 10f;
    private Vector3 moveDirection;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform groundCheck;

    public Rigidbody rb;
    private CapsuleCollider capsule;
    public Animator animator;
    public bool isBlocked = false;


    private Vector2 input;

    private bool isRunning;
    private bool jumpPressed;
    private bool isGrounded;

    public float currentSpeed;
    public bool IsMoving =>
    new Vector2(input.x, input.y).sqrMagnitude > 0.01f;


    private readonly Collider[] groundHits = new Collider[8];



    #region Unity Methods

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        ReadInput();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Move();
        RotatePlayer();
    }


    #endregion

    #region Functions

    private void ReadInput()
    {
        if (InventoryUI.IsInputBlocked)
        {
            input = Vector2.zero;
            isRunning = false;
            jumpPressed = false;
            return;
        }
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        input = Vector2.ClampMagnitude(input, 1f);

        isRunning = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isBlocked) return;
            jumpPressed = true;

        }
    }
    private void Move()
    {
        if(isBlocked) return;
        currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        moveDirection = forward * input.y + right * input.x;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        Vector3 platformVelocity = Vector3.zero;


        Vector3 playerMovement =
            moveDirection * currentSpeed;

        Vector3 velocity = rb.linearVelocity;

        velocity.x = playerMovement.x + platformVelocity.x;
        velocity.z = playerMovement.z + platformVelocity.z;

        rb.linearVelocity = velocity;
    }
    private void RotatePlayer()
    {
        if (moveDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime));
    }
    private void UpdateAnimator()
    {
        if (isBlocked) return;
        Vector3 localMove =
            transform.InverseTransformDirection(
                moveDirection * currentSpeed
            );

        float inputMagnitude = input.magnitude;

        animator.SetFloat(
            "MoveX",
            Mathf.Clamp(localMove.x / runSpeed, -1f, 1f),
            0.1f,
            Time.deltaTime
        );

        animator.SetFloat(
            "MoveZ",
            Mathf.Clamp(localMove.z / runSpeed, -1f, 1f),
            0.1f,
            Time.deltaTime
        );

        float animationSpeed = 0f;

        if (inputMagnitude > 0.01f)
            animationSpeed = isRunning ? 1f : 0.5f;

        animator.SetFloat(
            "Speed",
            animationSpeed,
            0.1f,
            Time.deltaTime
        );

        animator.SetBool("Grounded", isGrounded);
    }




    #endregion




}