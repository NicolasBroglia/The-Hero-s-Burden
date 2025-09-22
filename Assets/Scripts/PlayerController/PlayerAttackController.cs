using UnityEngine;
using System.Collections;

public class PlayerAttackController : MonoBehaviour
{
    public float AttackDuration => attackDuration;

    [Header("References")]
    [SerializeField] private CursorController cursorController;
    [SerializeField] private PlayerStateController stateController;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject meleeHitboxPrefab;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float attackDuration = 5f;

    private float nextAttackTime = 0f;

    private void Update()
    {
        Debug.Log(stateController.CurrentState);
        HandleAttack();
    }

    private void HandleAttack()
    {
        if (!stateController.CanAttack() || Time.time < nextAttackTime) return;

        if (Input.GetButtonDown("Fire1"))
        {
            Vector3 targetPosition = cursorController.GetCursorWorldPosition();
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f;

            transform.forward = direction;
            stateController.SetState(PlayerState.Attacking);

            SpawnMeleeHitbox(direction);

            nextAttackTime = Time.time + 1f / attackRate;
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
        if (stateController.CurrentState == PlayerState.Attacking)
            stateController.SetState(PlayerState.Idle);
    }
}
