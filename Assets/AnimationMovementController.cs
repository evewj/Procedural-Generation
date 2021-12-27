using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationMovementController : MonoBehaviour
{
    private PlayerInput playerInput;
    private CharacterController characterController;
    private Animator animator;

    int isWalkingHash;
    int isRunningHash;
    int isJumpingHash;
    int jumpCountHash;

    private Vector2 currentMovementInput;
    private Vector3 currentMovement;
    private Vector3 currentRunMovement;
    private bool isMovementPressed;
    private bool isRunPressed;
    private bool isJumpPressed = false;
    private bool isJumping = false;
    private bool isJumpAnimating = false;
    private int jumpCount = 0;
    private Dictionary<int, float> initialJumpVelocities = new Dictionary<int, float>();
    private Dictionary<int, float> jumpGravities = new Dictionary<int, float>();
    private Coroutine currentJumpResetCoroutine = null;

    public float rotationFactorPerFrame = 1.0f;
    public float runMultiplier = 3.0f;
    public float groundedGravity = -0.05f;
    public float gravity = -9.8f;

    public float initialJumpVelocity;
    public float maxJumpHeight = 1.5f;
    public float maxJumpTime = 0.5f; // s
    public float fallMultiplier = 1.5f;
    public float maxFallSpeed = -20.0f;
    public float multiJumpWindow = 0.5f;

    private static int maxJumps = 3;       

    private void Awake()
    {
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");
        jumpCountHash = Animator.StringToHash("jumpCount");

        playerInput.CharacterControls.Move.started += OnMovementInput;
        playerInput.CharacterControls.Move.canceled += OnMovementInput;
        playerInput.CharacterControls.Move.performed += OnMovementInput;
        playerInput.CharacterControls.Run.started += OnRun;
        playerInput.CharacterControls.Run.canceled += OnRun;
        playerInput.CharacterControls.Jump.started += OnJump;
        playerInput.CharacterControls.Jump.canceled += OnJump;

        SetupJumpVariables();
    }

    private void SetupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        float firstJumpGravity = -2 * maxJumpHeight / Mathf.Pow(timeToApex, 2);
        float firstJumpInitialJumpVelocity = 2 * maxJumpHeight / timeToApex;
        float secondJumpGravity = -2 * maxJumpHeight * 2 / Mathf.Pow(timeToApex * 1.25f, 2);
        float secondJumpInitialVelocity = 2 * maxJumpHeight * 2 / (timeToApex * 1.25f);
        float thirdJumpGravity = -2 * maxJumpHeight * 4 / Mathf.Pow(timeToApex * 1.5f, 2);
        float thirdJumpInitialVelocity = 2 * maxJumpHeight * 4 / (timeToApex * 1.5f);

        initialJumpVelocities.Add(1, firstJumpInitialJumpVelocity);
        initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        jumpGravities.Add(0, firstJumpGravity);
        jumpGravities.Add(1, firstJumpGravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
    }

    private void OnMovementInput(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        currentMovement = transform.right * currentMovementInput.x + transform.forward * currentMovementInput.y;
        currentRunMovement = runMultiplier * currentMovement;
        isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0;
    }

    private void OnRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    private void HandleJump()
    {
        // Debug.Log($"Is Jumping: {isJumping}");
        // Debug.Log($"Is Jump Pressed: {isJumpPressed}");        

        if (!isJumping && characterController.isGrounded && isJumpPressed)
        {
            if (jumpCount < maxJumps && currentJumpResetCoroutine != null)
            {
                StopCoroutine(currentJumpResetCoroutine);
            }
            animator.SetBool(isJumpingHash, true);
            isJumpAnimating = true;
            isJumping = true;
            jumpCount += 1;
            animator.SetInteger(jumpCountHash, jumpCount);
            currentMovement.y = initialJumpVelocities[jumpCount] * 0.5f; // assume previous y velcoity is 0
            currentRunMovement.y = initialJumpVelocities[jumpCount] * 0.5f;
        }
        else if (!isJumpPressed && isJumping && characterController.isGrounded)
        {
            isJumping = false;
        }
    }

    private IEnumerator JumpResetRoutine()
    {
        yield return new WaitForSeconds(multiJumpWindow);
        jumpCount = 0;
    }

    private void HandleGravity()
    {
        bool isFalling = currentMovement.y <= 0.0f || !isJumpPressed;

        if (characterController.isGrounded)
        {
            currentMovement.y = groundedGravity;
            currentRunMovement.y = groundedGravity;
            if (isJumpAnimating)
            {
                animator.SetBool(isJumpingHash, false);
                isJumpAnimating = false;
                currentJumpResetCoroutine = StartCoroutine(JumpResetRoutine());
                if(jumpCount == maxJumps)
                {
                    jumpCount = 0;
                    animator.SetInteger(jumpCountHash, jumpCount);
                }
            }           
        }
        else if (isFalling)
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (jumpGravities[jumpCount] * Time.deltaTime * fallMultiplier);
            float nextYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }
        else
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (jumpGravities[jumpCount] * Time.deltaTime);
            float nextYVelocity = Mathf.Max((previousYVelocity + newYVelocity) * 0.5f, maxFallSpeed); // Clamp for terminal velocity
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }
    }

    private void HandleRotation()
    {
        Vector3 positionToLookAt;       

        positionToLookAt.x = currentMovement.x;
        positionToLookAt.y = 0;
        positionToLookAt.z = currentMovement.z;

        Quaternion currentRotation = transform.rotation;

        if(isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }

    private void HandleAnimation()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);

        if(isMovementPressed && !isWalking)
        {
            animator.SetBool(isWalkingHash, true);
        }
        else if(!isMovementPressed && isWalking)
        {
            animator.SetBool(isWalkingHash, false);
        }

        if(isMovementPressed && !isRunning)
        {
            animator.SetBool(isRunningHash, true);
        }
        else if(!isMovementPressed || !isRunPressed && isRunning)
        {
            animator.SetBool(isRunningHash, false);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        HandleAnimation();
        HandleRotation();       
        if (isRunPressed)
        {
            characterController.Move(currentRunMovement * Time.deltaTime);
        }
        else
        {
            characterController.Move(currentMovement * Time.deltaTime);
        }
        HandleGravity();
        HandleJump();
    }

    private void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }

    private void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
}
