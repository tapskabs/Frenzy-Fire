using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Import the SceneManagement namespace

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

    private int currentEnemyIndex = 0;

    // The name of the next scene to load after the game ends
    public string nextSceneName;

    // Cooldown tracker for the special ability
    private int specialCooldown = 0;
    private const int specialCooldownDuration = 3;
    private int currentTurn = 0;

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
    }

    IEnumerator PlayerSpecialAbility()
    {
        bool isDead = enemyUnit.TakeDamage(playerUnit.damage * 2); // Special ability deals double damage

        enemyHUD.SetHP(enemyUnit.currentHP);
        dialogueText.text = "Special ability attack is successful!";

        specialCooldown = specialCooldownDuration; // Reset cooldown

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
    }

    void EndBattle()
    {
        if (state == BattleState.WON)
        {
            dialogueText.text = "You won the battle!";
            if (currentEnemyIndex >= enemyPrefabs.Length)
            {
                dialogueText.text = "You defeated all enemies!";
                // Load the next scene after a delay to show the message
                Invoke("LoadNextScene", 2f);
                return;
            }
           // playerUnit.LevelUp(5); // Increase player level by 5
            dialogueText.text += " You leveled up!";
            playerHUD.SetHUD(playerUnit); // Update player HUD to reflect new stats
            SpawnNextEnemy();
            state = BattleState.START;
            StartCoroutine(SetupBattle());
        }
        else if (state == BattleState.LOST)
        {
            dialogueText.text = "You were defeated.";
            // Load the next scene after a delay to show the message
            Invoke("LoadNextScene", 2f);
        }
    }

    void PlayerTurn()
    {
        dialogueText.text = "Choose an action:";
        currentTurn++;
        if (specialCooldown > 0)
        {
            specialCooldown--;
        }
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

    public void OnSpecialAbilityButton()
    {
        if (state != BattleState.PLAYERTURN)
            return;

        if (specialCooldown > 0)
        {
            dialogueText.text = $"Special ability is on cooldown. {specialCooldown} turn(s) remaining.";
            return;
        }

        StartCoroutine(PlayerSpecialAbility());
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
