using UnityEngine;
using DG.Tweening;

public class Rock : NatureObject
{
    [Header("Rock Settings")]
    [SerializeField] private int maxHits = 3;

    [SerializeField] private GameObject dropPoint;


    [Header("Break VFX")]
    [SerializeField] private ParticleSystem breakVFX;


    [Header("Drops")]
    [SerializeField] private ResourceDrop[] drops;


    [Header("Drop Spawn")]
    [SerializeField] private float dropRadius = 1.5f;


    private int currentHits;
    private bool isBroken;


    // =========================================================
    // INTERACTION
    // =========================================================

    public override void Interact(
        PlayerToolController player)
    {
        if (isBroken)
            return;


        currentHits++;


        Debug.Log(
            "Taþa vuruldu! " +
            currentHits +
            "/" +
            maxHits
        );


        HitRock();


        if (currentHits >= maxHits)
        {
            BreakRock();
        }
    }


    // =========================================================
    // HIT ANIMATION
    // =========================================================

    private void HitRock()
    {
        // Þimdilik boþ.
        // Ýleride taþýn her vuruþta
        // sarsýlmasý / animasyon oynamasý burada yapýlacak.
    }


    // =========================================================
    // BREAK
    // =========================================================

    private void BreakRock()
    {
        if (isBroken)
            return;


        isBroken = true;


        PlayBreakVFX();

        SpawnDrops();


        // Taþý yok et
        Destroy(gameObject);
    }


    // =========================================================
    // VFX
    // =========================================================

    private void PlayBreakVFX()
    {
        if (breakVFX == null)
            return;


        // VFX taþla birlikte yok olmasýn.
        breakVFX.transform.SetParent(null);


        breakVFX.Play();


        ParticleSystem.MainModule main =
            breakVFX.main;


        Destroy(
            breakVFX.gameObject,
            main.duration +
            main.startLifetime.constantMax
        );
    }


    // =========================================================
    // DROPS
    // =========================================================

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


            for (int i = 0;
                 i < drop.spawnCount;
                 i++)
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


    // =========================================================
    // DROP POSITION
    // =========================================================

    private Vector3 GetRandomDropPosition()
    {
        Vector2 randomCircle =
            Random.insideUnitCircle *
            dropRadius;


        Vector3 centerPosition =
            dropPoint != null
                ? dropPoint.transform.position
                : transform.position;


        return centerPosition +
               new Vector3(
                   randomCircle.x,
                   0.3f,
                   randomCircle.y
               );
    }


    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}