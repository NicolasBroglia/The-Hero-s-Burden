using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    public Vector3 size = new Vector3(2f, 1f, 1f);
    public LayerMask affectedLayers; // specify which layers are affected

    private BoxCollider box;

    private void Awake()
    {
        box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true; // ensures no physics push
        box.size = size;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & affectedLayers) != 0)
        {
            // Object is in affected layers → apply logic
            Debug.Log("Hit object: " + other.name);
            // Example: apply damage, knockback, etc.
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
