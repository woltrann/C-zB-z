using UnityEngine;
using DG.Tweening;

public class Tree : NatureObject
{
    [Header("Tree Settings")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private GameObject dropPoint;

    [Header("Animation Object")]
    [SerializeField] private GameObject animationObject;

    [Header("Stump")]
    [SerializeField] private GameObject stumpPrefab;

    [Header("Fall VFX")]
    [SerializeField] private ParticleSystem fallVFX;

    [Header("Drops")]
    [SerializeField] private ResourceDrop[] drops;

    [Header("Drop Spawn")]
    [SerializeField] private float dropRadius = 1.5f;

    [Header("Hit Animation")]
    [SerializeField] private float shakeAngle = 5f;

    [SerializeField] private float shakeDuration = 0.1f;

    [Header("Fall Animation")]
    [SerializeField] private float fallAngle = 90f;

    [SerializeField] private float fallDuration = 1f;

    private int currentHits;
    private bool isFalling;

    private Vector3 originalLocalEulerAngles;


    private void Awake()
    {
        if (animationObject == null)
        {
            Debug.LogError(
                "Tree: Animation Object atanmadý!",
                this
            );

            return;
        }

        originalLocalEulerAngles =
            animationObject.transform.localEulerAngles;
    }


    public override void Interact(
        PlayerToolController player)
    {
        if (isFalling)
            return;

        currentHits++;

        Debug.Log(
            "Aðaca vuruldu! " +
            currentHits +
            "/" +
            maxHits
        );

        HitTree();

        if (currentHits >= maxHits)
        {
            FallTree();
        }
    }


    private void HitTree()
    {
        if (animationObject == null)
            return;

        Transform target =
            animationObject.transform;

        target.DOKill();

        target.localEulerAngles =
            originalLocalEulerAngles;

        Sequence sequence =
            DOTween.Sequence();

        sequence.Append(
            target.DOLocalRotate(
                originalLocalEulerAngles +
                new Vector3(
                    shakeAngle,
                    0f,
                    0f                    
                ),
                shakeDuration
            )
        );

        sequence.Append(
            target.DOLocalRotate(
                originalLocalEulerAngles +
                new Vector3(
                    -shakeAngle,
                    0f,
                    0f
                ),
                shakeDuration
            )
        );

        sequence.Append(
            target.DOLocalRotate(
                originalLocalEulerAngles,
                shakeDuration
            )
        );
    }


    private void FallTree()
    {
        isFalling = true;

        if (animationObject == null)
            return;

        Transform target =
            animationObject.transform;

        target.DOKill();

        target.DOLocalRotate(
            originalLocalEulerAngles +
            new Vector3(
                fallAngle,
                0f,
                0f
            ),
            fallDuration
        )
        .SetEase(Ease.InQuad)
        .OnComplete(OnTreeFallen);
    }


    private void OnTreeFallen()
    {
        
        //Destroy(animationObject);

        PlayFallVFX();

        SpawnStump();

        SpawnDrops();
        animationObject.SetActive(false);
    }


    private void PlayFallVFX()
    {
        Debug.Log("vfx baþladý");
        if (fallVFX == null)
            return;

        fallVFX.transform.SetParent(null);

        fallVFX.Play();

        Destroy(
            fallVFX.gameObject,
            fallVFX.main.duration +
            fallVFX.main.startLifetime.constantMax
        );
        Debug.Log("vfx bitti");

    }


    private void SpawnStump()
    {
        if (stumpPrefab == null)
            return;

        Instantiate(
            stumpPrefab,
            dropPoint.transform.position,
            animationObject.transform.rotation
        );
    }


    private void SpawnDrops()
    {
        if (drops == null)
            return;

        foreach (ResourceDrop drop in drops)
        {
            if (drop == null)
                continue;

            if (drop.item == null)
                continue;

            if (drop.dropPrefab == null)
                continue;

            for (int i = 0; i < drop.spawnCount; i++)
            {
                Vector3 spawnPosition =
                    GetRandomDropPosition();

                GameObject spawnedObject =
                    Instantiate(
                        drop.dropPrefab,
                        spawnPosition,
                        Quaternion.Euler(
                            0f,
                            Random.Range(
                                0f,
                                360f
                            ),
                            0f
                        )
                    );

                WorldItem worldItem =
                    spawnedObject.GetComponent<WorldItem>();

                if (worldItem == null)
                {
                    Debug.LogWarning(
                        "Drop prefab üzerinde " +
                        "WorldItem yok: " +
                        spawnedObject.name
                    );

                    continue;
                }

                worldItem.Initialize(
                    drop.item,
                    drop.amount
                );
            }
        }
    }


    private Vector3 GetRandomDropPosition()
    {
        Vector2 randomCircle =
            Random.insideUnitCircle *
            dropRadius;

        Vector3 position =
            dropPoint.transform.position +
            new Vector3(
                randomCircle.x,
                0.3f,
                randomCircle.y
            );

        return position;
    }


    private void OnDestroy()
    {
        if (animationObject != null)
        {
            animationObject.transform.DOKill();
        }
    }
}