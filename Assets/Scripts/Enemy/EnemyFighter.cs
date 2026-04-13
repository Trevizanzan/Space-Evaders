using UnityEngine;
using System.Collections;

public class EnemyFighter : EnemyBase
{
    [Header("Fighter Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float diagonalAngle = 35f;  // gradi di inclinazione traiettoria diagonale
    [SerializeField] private float sCurveFrequency = 1.2f; // frequenza oscillazione traiettoria a S
    [SerializeField] private float sCurveAmplitude = 2.5f; // ampiezza oscillazione traiettoria a S
    //[SerializeField] private float sCurveFrequencyMin = 0.8f;
    //[SerializeField] private float sCurveFrequencyMax = 2f;
    //[SerializeField] private float sCurveAmplitudeMin = 1.5f;
    //[SerializeField] private float sCurveAmplitudeMax = 3.5f;

    [Header("Trajectory Settings")]
    [SerializeField] private bool allowStraight = true;
    [SerializeField] private bool allowDiagonal = true;
    [SerializeField] private bool allowSCurve = true;

    [Header("Attack")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootIntervalMin = 1.5f;
    [SerializeField] private float shootIntervalMax = 3.5f;
    [SerializeField] private Transform firePoint;           // punto di spawn del proiettile (figlio del prefab)

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 120f;    // gradi/secondo verso il player

    // Traiettoria assegnata allo spawn
    private enum TrajectoryType { Straight, Diagonal, SCurve }
    private TrajectoryType trajectory;

    // Stato movimento
    private Vector3 moveDirection;      // usato da Straight e Diagonal
    private float sCurveTime = 0f;    // usato da SCurve
    private float sCurveDir = 1f;    // 1 = parte verso destra, -1 verso sinistra
    private float startX;             // X iniziale (per SCurve)

    // Stato attacco
    private float shootTimer;
    private float currentShootInterval;

    public void Initialize(PhaseConfig phase)
    {
        moveSpeed = moveSpeed * phase.fighterSpeedMult;
        shootIntervalMin = shootIntervalMin * phase.fighterShootRateMult;
        shootIntervalMax = shootIntervalMax * phase.fighterShootRateMult;
    }

    protected override void Start()
    {
        base.Start();
        currentShootInterval = Random.Range(shootIntervalMin, shootIntervalMax);

        AssignTrajectory();
    }

    void AssignTrajectory()
    {
        // Costruisce la lista delle traiettorie abilitate
        var allowed = new System.Collections.Generic.List<TrajectoryType>();
        if (allowStraight) allowed.Add(TrajectoryType.Straight);
        if (allowDiagonal) allowed.Add(TrajectoryType.Diagonal);
        if (allowSCurve) allowed.Add(TrajectoryType.SCurve);

        // Fallback: se nessuna è abilitata usa Straight
        if (allowed.Count == 0) allowed.Add(TrajectoryType.Straight);

        trajectory = allowed[Random.Range(0, allowed.Count)];
        startX = transform.position.x;

        switch (trajectory)
        {
            case TrajectoryType.Straight:
                moveDirection = Vector3.down;
                break;

            case TrajectoryType.Diagonal:
                float side = transform.position.x > 0 ? -1f : 1f;
                float rad = diagonalAngle * Mathf.Deg2Rad;
                moveDirection = new Vector3(
                    Mathf.Sin(rad) * side,
                   -Mathf.Cos(rad),
                    0f).normalized;
                break;

            case TrajectoryType.SCurve:
                sCurveDir = Random.value > 0.5f ? 1f : -1f;
                // sCurveFrequency e sCurveAmplitude vengono dai SerializeField
                // sCurveFrequency = Random.Range(sCurveFrequencyMin, sCurveFrequencyMax);
                // sCurveAmplitude = Random.Range(sCurveAmplitudeMin, sCurveAmplitudeMax);
                break;
        }
    }

    protected override void UpdateBehavior()
    {
        Move();
        RotateTowardPlayer();
        HandleShooting();
    }

    void Move()
    {
        switch (trajectory)
        {
            case TrajectoryType.Straight:
            case TrajectoryType.Diagonal:
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
                break;

            case TrajectoryType.SCurve:
                sCurveTime += Time.deltaTime;
                float offsetX = Mathf.Sin(sCurveTime * sCurveFrequency * Mathf.PI) * sCurveAmplitude * sCurveDir;
                float newX = startX + offsetX;
                float newY = transform.position.y - moveSpeed * Time.deltaTime;
                transform.position = new Vector3(newX, newY, transform.position.z);
                break;
        }
    }

    void RotateTowardPlayer()
    {
        if (playerTransform == null) return;

        Vector2 dir = (playerTransform.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.z;

        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    void HandleShooting()
    {
        shootTimer += Time.deltaTime;
        if (shootTimer < currentShootInterval) return;

        Shoot();
        shootTimer = 0f;
        currentShootInterval = Random.Range(shootIntervalMin, shootIntervalMax);
    }

    void Shoot()
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        // Il proiettile parte verso il player al momento dello sparo
        Vector3 shootDir = playerTransform != null
            ? (playerTransform.position - spawnPos).normalized
            : Vector3.down;

        // Ruota il bullet nella direzione di sparo
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        GameObject bullet = Instantiate(
            bulletPrefab,
            spawnPos,
            Quaternion.Euler(0f, 0f, angle));

        // Imposta velocità direttamente sul Rigidbody2D se presente,
        // altrimenti EnemyBullet userà il suo Vector3.down di default
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = shootDir * bullet.GetComponent<EnemyBullet>().GetSpeed();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayShoot();  // TODO: teniamo?
    }
}