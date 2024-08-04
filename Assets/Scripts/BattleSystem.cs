using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST }

public class BattleSystem : MonoBehaviour
{
    public GameObject playerPrefab;

    // Array to hold different enemy prefabs
    public GameObject[] enemyPrefabs;

    public Transform playerBattleStation;
    public Transform enemyBattleStation;

    Unit playerUnit;
    Unit enemyUnit;

    public Text dialogueText;

    public BattleHUD playerHUD;
    public BattleHUD enemyHUD;

    public BattleState state;

    private int currentEnemyIndex;
    private int specialCooldown = 0; // Special ability cooldown
    private int turnsSinceSpecial = 0; // Turns since special was last used

    // The name of the next scene to load after the game ends
    public string nextSceneName;

    // Start is called before the first frame update
    void Start()
    {
        state = BattleState.START;
        StartCoroutine(SetupBattle());
    }

    IEnumerator SetupBattle()
    {
        GameObject playerGO = Instantiate(playerPrefab, playerBattleStation);
        playerUnit = playerGO.GetComponent<Unit>();

        // Load player stats
        playerUnit.unitLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        playerUnit.damage = PlayerPrefs.GetInt("PlayerDamage", 10);
        playerUnit.maxHP = PlayerPrefs.GetInt("PlayerMaxHP", 100);
        playerUnit.currentHP = PlayerPrefs.GetInt("PlayerCurrentHP", 100);

        // Load the current enemy index
        currentEnemyIndex = PlayerPrefs.GetInt("CurrentEnemyIndex", 0);

        SpawnNextEnemy();

        dialogueText.text = "A wild " + enemyUnit.unitName + " approaches...";

        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);

        yield return new WaitForSeconds(2f);

        state = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    void SpawnNextEnemy()
    {
        if (currentEnemyIndex >= enemyPrefabs.Length)
        {
            EndBattle();
            return;
        }

        // Clear the previous enemy if it exists
        if (enemyBattleStation.childCount > 0)
        {
            foreach (Transform child in enemyBattleStation)
            {
                Destroy(child.gameObject);
            }
        }

        // Instantiate the next enemy prefab
        GameObject enemyGO = Instantiate(enemyPrefabs[currentEnemyIndex], enemyBattleStation);
        enemyUnit = enemyGO.GetComponent<Unit>();

        enemyHUD.SetHUD(enemyUnit);
        currentEnemyIndex++;
    }

    IEnumerator PlayerAttack()
    {
        bool isDead = enemyUnit.TakeDamage(playerUnit.damage);

        enemyHUD.SetHP(enemyUnit.currentHP);
        dialogueText.text = "The attack is successful!";

        yield return new WaitForSeconds(2f);

        if (isDead)
        {
            state = BattleState.WON;
            EndBattle();
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }

        turnsSinceSpecial++; // Increment turn counter for special cooldown
    }

    IEnumerator EnemyTurn()
    {
        dialogueText.text = enemyUnit.unitName + " attacks!";

        yield return new WaitForSeconds(1f);

        bool isDead = playerUnit.TakeDamage(enemyUnit.damage);

        playerHUD.SetHP(playerUnit.currentHP);

        yield return new WaitForSeconds(1f);

        if (isDead)
        {
            state = BattleState.LOST;
            EndBattle();
        }
        else
        {
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }

        turnsSinceSpecial++; // Increment turn counter for special cooldown
    }

    void EndBattle()
    {
        if (state == BattleState.WON)
        {
            dialogueText.text = "You won the battle!";

            // Level up the player
            playerUnit.unitLevel += enemyUnit.unitLevel; // Increase player level by enemy level
            playerUnit.damage += 5; // Increase player damage by 5
            playerUnit.maxHP += 5; // Increase player max HP by 5

            // Save player stats
            PlayerPrefs.SetInt("PlayerLevel", playerUnit.unitLevel);
            PlayerPrefs.SetInt("PlayerDamage", playerUnit.damage);
            PlayerPrefs.SetInt("PlayerMaxHP", playerUnit.maxHP);
            PlayerPrefs.SetInt("PlayerCurrentHP", playerUnit.currentHP);

            // Save the index of the enemy being fought
            PlayerPrefs.SetInt("CurrentEnemyIndex", currentEnemyIndex);

            playerHUD.SetHUD(playerUnit); // Update player HUD to reflect new stats

            // Load the next scene after a delay to show the message
            Invoke("LoadNextScene", 2f);
        }
        else if (state == BattleState.LOST)
        {
            dialogueText.text = "You were defeated.";
            // Reset player stats
            PlayerPrefs.DeleteAll();
            // Load the next scene after a delay to show the message
            Invoke("LoadNextScene", 2f);
        }
    }

    void PlayerTurn()
    {
        dialogueText.text = "Choose an action:";
    }

    IEnumerator PlayerHeal()
    {
        playerUnit.Heal(5);

        playerHUD.SetHP(playerUnit.currentHP);
        dialogueText.text = "You feel renewed strength!";

        yield return new WaitForSeconds(2f);

        state = BattleState.ENEMYTURN;
        StartCoroutine(EnemyTurn());
    }

    IEnumerator PlayerSpecial()
    {
        specialCooldown = 3; // Set cooldown for special attack
        turnsSinceSpecial = 0; // Reset turn counter for special attack
        playerUnit.damage += 10; // Increase player damage by 10 for the special attack
        dialogueText.text = "Special attack used! Damage increased.";

        yield return new WaitForSeconds(2f);

        state = BattleState.ENEMYTURN;
        StartCoroutine(EnemyTurn());
    }

    public void OnAttackButton()
    {
        if (state != BattleState.PLAYERTURN)
            return;

        StartCoroutine(PlayerAttack());
    }

    public void OnHealButton()
    {
        if (state != BattleState.PLAYERTURN)
            return;

        StartCoroutine(PlayerHeal());
    }

    public void OnSpecialButton()
    {
        if (state != BattleState.PLAYERTURN)
            return;

        if (turnsSinceSpecial < 3)
        {
            dialogueText.text = "Special attack on cooldown. Wait " + (3 - turnsSinceSpecial) + " more turns.";
            return;
        }

        StartCoroutine(PlayerSpecial());
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene("TopDownScene");
    }
}
