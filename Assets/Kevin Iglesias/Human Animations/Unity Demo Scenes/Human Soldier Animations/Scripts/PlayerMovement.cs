using KevinIglesias;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Movement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cam;
    public Transform player;

    public float speed = 10f;
    public float gravity = -9.81f;
    Vector3 velocity;

    public Transform groundCheck;
    public float groundRadius = 0.5f;
    public LayerMask groundMask;
    public bool isGrounded;

    public float turnSmoothTime = 0.1f;
    public float turnSmoothSpeed = 1.0f;
    public float lookSensitivity = 0.5f;
    public Vector3 camOffset = new Vector3(5, 7, -5);

    public float jumpHeight = 1f;
    public float jumpModifier = 1f;

    private PlayerInputActions input;
    private HumanSoldierController soldier;
    private Vector2 moveInput;
    private bool firePressed;
    private bool jumpPressed;

    [Header("Bullet")]
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float shot_cooldown = 10f;
    public ParticleSystem muzzleflash;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        soldier = GetComponent<HumanSoldierController>();

    }
    void Awake()
    {
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Player.Enable();
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        moveInput = input.Player.Move.ReadValue<Vector2>();
        Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        float targetAngle_H = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
        float angle_H = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle_H, ref turnSmoothSpeed, turnSmoothTime);

        if (direction.magnitude >= 0.05f)
        {
            transform.rotation = Quaternion.Euler(0f, angle_H, 0f);
            Vector3 moveDirn = transform.forward;
            soldier.movement = SoldierMovement.Run;
            controller.Move(moveDirn.normalized * speed * Time.deltaTime);     
        }
        else
            soldier.movement = SoldierMovement.NoMovement;

        if (input.Player.Jump.WasPressedThisFrame() && isGrounded)
        {
            soldier.action = SoldierAction.Jump;
            velocity.y = Mathf.Sqrt((jumpHeight * 2 * -gravity) * jumpModifier);
        }

        else if (input.Player.Fire.WasPressedThisFrame())
        {
            // Trigger shooting once
            if (soldier.action != SoldierAction.Shoot01)
                soldier.action = SoldierAction.Shoot01;

            BulletSpawner();
            BulletSpawner(0, 5, 0);
            BulletSpawner(0, -5, 0);
        }
        else
            soldier.action = SoldierAction.Nothing;

            velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    void BulletSpawner(float i = 0f, float j = 0f, float k = 0f)
    {
        bulletSpawnPoint.Rotate(i, j, k, Space.Self);

        var bullet_0 = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bullet_0.GetComponent<Rigidbody>().linearVelocity = bulletSpawnPoint.forward * bulletSpeed;
        muzzleflash.Play();

        bulletSpawnPoint.Rotate(-i, -j, -k, Space.Self);
    }
 
    
}
