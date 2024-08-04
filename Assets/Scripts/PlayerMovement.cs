using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    public Animator animator;

    public Unit playerUnit;
    public PlayerHUD playerHUD;

    Vector2 movement;
    private bool canCollide = true;

    void Start()
    {
        // Initialize player stats if needed
        playerUnit.unitLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        playerUnit.damage = PlayerPrefs.GetInt("PlayerDamage", 10);
        playerUnit.maxHP = PlayerPrefs.GetInt("PlayerMaxHP", 100);
        playerUnit.currentHP = PlayerPrefs.GetInt("PlayerCurrentHP", 100);

        // Update the PlayerHUD
        if (playerHUD != null)
        {
            playerHUD.SetHUD(playerUnit);
        }

        // Load player position if needed
        float playerX = PlayerPrefs.GetFloat("PlayerX", transform.position.x);
        float playerY = PlayerPrefs.GetFloat("PlayerY", transform.position.y);
        transform.position = new Vector2(playerX, playerY);

        StartCoroutine(EnableCollisionAfterDelay(1f));
    }

    void Update()
    {
        // Input
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);
    }

    void FixedUpdate()
    {
        // Movement
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") && canCollide)
        {
            // Save player position
            PlayerPrefs.SetFloat("PlayerX", transform.position.x);
            PlayerPrefs.SetFloat("PlayerY", transform.position.y);

            // Save player stats
            PlayerPrefs.SetInt("PlayerLevel", playerUnit.unitLevel);
            PlayerPrefs.SetInt("PlayerDamage", playerUnit.damage);
            PlayerPrefs.SetInt("PlayerMaxHP", playerUnit.maxHP);
            PlayerPrefs.SetInt("PlayerCurrentHP", playerUnit.currentHP);

            // Save the index of the enemy being fought
            Enemy enemy = collision.GetComponent<Enemy>();
            PlayerPrefs.SetInt("CurrentEnemyIndex", enemy.enemyIndex);

            // Load the battle scene
            SceneManager.LoadScene("BattleScene");
        }
    }

    IEnumerator EnableCollisionAfterDelay(float delay)
    {
        canCollide = false;
        yield return new WaitForSeconds(delay);
        canCollide = true;
    }
}
