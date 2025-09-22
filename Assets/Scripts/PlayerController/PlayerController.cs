using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Air Control")]
    [Range(0f, 1f)][SerializeField] private float airControl = 0.3f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerStateController stateController;
    [SerializeField] private PlayerAttackController playerAttackController;


   [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float attackMoveReduction = 0.5f; // reduce speed while attacking
    [SerializeField] private float attackMoveSpeed = 1f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f; // adjustable

    [Header("Jump Settings")]
    [SerializeField] private float minJumpHeight = 2f;
    [SerializeField] private float maxJumpHeight = 4f;
    [SerializeField] private float maxJumpHoldTime = 0.25f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 8f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping = false;
    private float jumpHoldTimer = 0f;
    private bool isDashing = false;
    private bool dashOnCooldown = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (stateController == null)
            stateController = GetComponent<PlayerStateController>();

        if (playerAttackController == null)
        {
            playerAttackController = GetComponent <PlayerAttackController>();
        }
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;

        HandleMovement();
        HandleGravity();
        HandleJump();
        HandleDash();
    }

    #region Movement
    private void HandleMovement()
    {
        // Instead of blocking all movement, just reduce speed if attacking
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude < 0.1f)
        {
            if (isGrounded) stateController.SetState(PlayerState.Idle);
            return;
        }

        float speed = moveSpeed;

        // Apply attack reduction but don't block movement
        if (stateController.CurrentState == PlayerState.Attacking)
            speed *= attackMoveReduction;

        float controlFactor = isGrounded ? 1f : airControl;
        controller.Move(inputDir * speed * controlFactor * Time.deltaTime);

        // Rotate smoothly
        Quaternion targetRotation = Quaternion.LookRotation(inputDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (isGrounded && stateController.CurrentState != PlayerState.Attacking)
        {
            stateController.SetState(PlayerState.Moving);
        }

        // reduce move speed until attack is done
        if (stateController.CurrentState == PlayerState.Attacking)
        {
            StartCoroutine(WaitForEndOfAttack());
            speed = attackMoveReduction;
        }
    }
    IEnumerator WaitForEndOfAttack()
    {
        Debug.Log("Waiting...");
        yield return new WaitForSeconds(playerAttackController.AttackDuration);
        if (isGrounded) stateController.SetState(PlayerState.Moving);
        Debug.Log("Done waiting!");
    }

    #endregion

    #region Jump
    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            isJumping = true;
            jumpHoldTimer = 0f;
            velocity.y = Mathf.Sqrt(minJumpHeight * -2f * gravity);
            stateController.SetState(PlayerState.Jumping);
        }

        if (isJumping && Input.GetButton("Jump"))
        {
            jumpHoldTimer += Time.deltaTime;
            if (jumpHoldTimer < maxJumpHoldTime)
            {
                float t = jumpHoldTimer / maxJumpHoldTime;
                float targetVelocity = Mathf.Sqrt(Mathf.Lerp(minJumpHeight, maxJumpHeight, t) * -2f * gravity);
                if (velocity.y < targetVelocity)
                    velocity.y = targetVelocity;
            }
        }

        if (Input.GetButtonUp("Jump") || jumpHoldTimer >= maxJumpHoldTime)
        {
            isJumping = false;
        }
    }
    #endregion

    #region Gravity
    private void HandleGravity()
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);
    }
    #endregion

    #region Dash
    private void HandleDash()
    {
        if (Input.GetButtonDown("Fire3") && !isDashing && !dashOnCooldown)
        {
            StartCoroutine(DashCoroutine());
        }
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        dashOnCooldown = true;
        stateController.SetState(PlayerState.Dashing);

        Vector3 dashDir = transform.forward;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            controller.Move(dashDir * (dashDistance / dashDuration) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        stateController.SetState(PlayerState.Idle);

        yield return new WaitForSeconds(dashCooldown);
        dashOnCooldown = false;
    }
    #endregion
}
