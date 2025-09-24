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

    [Header("Attack References")]
    [SerializeField] private CursorController cursorController;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject meleeHitboxPrefab;

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

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float attackDuration = 5f;

  //  private float nextAttackTime = 0f;

    private PlayerStateController playerStateController;
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping = false;
    private float jumpHoldTimer = 0f;
    private bool isDashing = false;
    private bool dashOnCooldown = false;
    private Vector3 inputDir;


    private void Awake()
    {

        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (stateController == null)
            stateController = GetComponent<PlayerStateController>();

        if (playerStateController == null)
        {
            playerStateController = GetComponent<PlayerStateController>();
        }


        
        if (isGrounded) 
            stateController.SetState(PlayerState.Idle);

        if (inputDir.magnitude < 0.1f)
        {
            if (isGrounded) stateController.SetState(PlayerState.Idle);
            return;
        }
    }
    

    private void Update()
    {
        isGrounded = controller.isGrounded;

        HandleMovement();
        HandleGravity();
        HandleJump();
        HandleDash();
        HandleAttack();
    }

    #region Movement
    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        // Instead of blocking all movement, just reduce speed if attacking
        inputDir = new Vector3(h, 0f, v).normalized;
        float speed = moveSpeed;

        // Apply attack reduction but don't block movement
        if (stateController.CurrentState == PlayerState.Attacking)
            speed *= attackMoveReduction;

        float controlFactor = isGrounded ? 1f : airControl;
        controller.Move(inputDir * speed * controlFactor * Time.deltaTime);

       
        // Rotate smoothly and limit rotation when attacking

        if (inputDir != Vector3.zero && playerStateController.CanRotate())
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (isGrounded && stateController.CurrentState != PlayerState.Attacking)
        {
            stateController.SetState(PlayerState.Moving);
        }

        // reduce move speed until attack is done and stop rotation
        if (stateController.CurrentState == PlayerState.Attacking)
        {
            speed = attackMoveReduction;
        }
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

    #region Attack

    private void HandleAttack()
    {
        if (!stateController.CanAttack() ) return;

        if (Input.GetButtonDown("Fire1"))
        {
            Vector3 targetPosition = cursorController.GetCursorWorldPosition();
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f;

            transform.forward = direction;
            stateController.SetState(PlayerState.Attacking);

            SpawnMeleeHitbox(direction);
            StartCoroutine(EndAttackCoroutine());
        }
    }

    private void SpawnMeleeHitbox(Vector3 direction)
    {
        if (attackPoint == null || meleeHitboxPrefab == null) return;

        Vector3 spawnPos = attackPoint.position + direction * attackRange * 0.5f;
        GameObject hitbox = Instantiate(meleeHitboxPrefab, spawnPos, Quaternion.LookRotation(direction));

        hitbox.transform.localScale = new Vector3(attackRange, 1f, attackRange);
        Destroy(hitbox, attackDuration); // matches your previous duration
    }

    private IEnumerator EndAttackCoroutine()
    {
        yield return new WaitForSeconds(attackDuration); // attack duration
        Debug.Log("ESPERA TERMINADA");
        if (stateController.CurrentState == PlayerState.Attacking)
            stateController.SetState(PlayerState.Idle);
    }

    #endregion
}
