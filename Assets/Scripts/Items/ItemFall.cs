using UnityEngine;

public class ItemFall : MonoBehaviour
{
    [Header("Temas Edince Rigidbody Kapatýlacak Layerlar")]
    [SerializeField] private LayerMask targetLayers;

    private Rigidbody rb;
    private Collider coll;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsInTargetLayer(collision.gameObject.layer))
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            coll.isTrigger = true;
        }
    }

    private bool IsInTargetLayer(int layer)
    {
        return (targetLayers.value & (1 << layer)) != 0;
    }
}
