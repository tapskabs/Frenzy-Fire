using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST }

public class BattleSystem : MonoBehaviour
{
    [Header("Prefabs / Transforms")]
    public GameObject playerPrefab;
    public GameObject[] enemyPrefabs; // last one should be boss prefab
    public Transform playerBattleStation;
    public Transform enemyBattleStation;

    [Header("UI")]
    public Text dialogueText;
    public BattleHUD playerHUD;
    public BattleHUD enemyHUD;
    public UpgradePanel upgradePanel; // drag your UpgradePanel here

    Unit playerUnit;
    Unit enemyUnit;

    public BattleState state;

    // special attack cooldown trackers
    private int turnsSinceSpecial = 3; // start able to use
    private const int requiredTurnsBetweenSpecial = 3;

    // single enemy per fight
    private GameObject currentEnemyGO;

    void Start()
    {
        state = BattleState.START;
        StartCoroutine(SetupBattle());
    }

    IEnumerator SetupBattle()
    {
        // If first time, instantiate player into station
        if (playerUnit == null)
        {
            GameObject playerGO = Instantiate(playerPrefab, playerBattleStation);
            playerUnit = playerGO.GetComponent<Unit>();
        }

        // Load player stats from GameManager singleton
        var gm = GameManager.Instance;
        playerUnit.unitLevel = gm.playerLevel;
        playerUnit.damage = gm.damage;
        playerUnit.maxHP = gm.maxHP;
        playerUnit.currentHP = gm.currentHP;

        // spawn a single enemy for this fight
        SpawnSingleEnemy();

        dialogueText.text = "A wild " + enemyUnit.unitName + " approaches...";
        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);

        yield return new WaitForSeconds(1.2f);

        state = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    void SpawnSingleEnemy()
    {
        // Clear existing enemy if present
        if (currentEnemyGO != null) Destroy(currentEnemyGO);

        // Determine whether to spawn boss: only spawn boss if player level > 50
        bool spawnBoss = GameManager.Instance.playerLevel > 50;

        GameObject prefabToSpawn;
        if (spawnBoss && enemyPrefabs.Length > 0)
        {
            prefabToSpawn = enemyPrefabs[enemyPrefabs.Length - 1]; // last = boss
        }
        else
        {
            // choose a random normal enemy from array excluding last element (boss)
            int maxIndex = Mathf.Max(0, enemyPrefabs.Length - 1);
            int idx = (maxIndex == 0) ? 0 : Random.Range(0, maxIndex);
            prefabToSpawn = enemyPrefabs[idx];
        }

        currentEnemyGO = Instantiate(prefabToSpawn, enemyBattleStation);
        enemyUnit = currentEnemyGO.GetComponent<Unit>();

        // Basic scaling so enemies are somewhat tied to player level
        int scaleLevel = Mathf.Max(1, GameManager.Instance.playerLevel + Random.Range(-2, 3));
        enemyUnit.unitLevel = scaleLevel;
        enemyUnit.maxHP = Mathf.Max(1, enemyUnit.maxHP + scaleLevel * 4);
        enemyUnit.currentHP = enemyUnit.maxHP;
        enemyUnit.damage = Mathf.Max(1, enemyUnit.damage + scaleLevel * 2);

        enemyHUD.SetHUD(enemyUnit);
    }

    IEnumerator PlayerAttack()
    {
        bool isDead = enemyUnit.TakeDamage(playerUnit.damage);
        enemyHUD.SetHP(enemyUnit.currentHP);
        dialogueText.text = "You hit for " + playerUnit.damage + " damage.";

        yield return new WaitForSeconds(1.2f);

        // increment turn counter for special cooldown (player action counts)
        turnsSinceSpecial++;

        if (isDead)
        {
            state = BattleState.WON;
            OnVictory();
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
    }

    IEnumerator PlayerSpecial()
    {
        // Check cooldown
        if (turnsSinceSpecial < requiredTurnsBetweenSpecial)
        {
            dialogueText.text = $"Special on cooldown. Wait {requiredTurnsBetweenSpecial - turnsSinceSpecial} turn(s).";
            yield break;
        }

        // Execute special: double damage attack for this action (doesn't permanently alter base damage)
        int specialDamage = playerUnit.damage * 2;
        bool isDead = enemyUnit.TakeDamage(specialDamage);
        enemyHUD.SetHP(enemyUnit.currentHP);
        dialogueText.text = "You used SPECIAL for " + specialDamage + " damage!";

        // reset counter
        turnsSinceSpecial = 0;

        yield return new WaitForSeconds(1.2f);

        if (isDead)
        {
            state = BattleState.WON;
            OnVictory();
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
    }

    IEnumerator PlayerHeal()
    {
        playerUnit.Heal(5);
        playerHUD.SetHP(playerUnit.currentHP);
        dialogueText.text = "You healed 5 HP.";

        yield return new WaitForSeconds(1.2f);

        // player used one turn
        turnsSinceSpecial++;

        state = BattleState.ENEMYTURN;
        StartCoroutine(EnemyTurn());
    }

    IEnumerator EnemyTurn()
    {
        // Randomly decide: heal by 8 or attack
        bool willHeal = Random.Range(0, 2) == 0;
        if (willHeal)
        {
            enemyUnit.Heal(8);
            enemyHUD.SetHP(enemyUnit.currentHP);
            dialogueText.text = $"{enemyUnit.unitName} healed 8 HP!";
            yield return new WaitForSeconds(1.0f);

            // enemy used a turn -> increment player's turnsSinceSpecial because turns are counted globally
            turnsSinceSpecial++;
            state = BattleState.PLAYERTURN;
            PlayerTurn();
            yield break;
        }
        else
        {
            dialogueText.text = $"{enemyUnit.unitName} attacks!";
            yield return new WaitForSeconds(1.0f);

            bool isDead = playerUnit.TakeDamage(enemyUnit.damage);
            playerHUD.SetHP(playerUnit.currentHP);
            dialogueText.text = $"{enemyUnit.unitName} hit you for {enemyUnit.damage} damage.";

            yield return new WaitForSeconds(1.0f);

            // enemy used a turn
            turnsSinceSpecial++;

            if (isDead)
            {
                state = BattleState.LOST;
                OnPlayerDefeated();
            }
            else
            {
                state = BattleState.PLAYERTURN;
                PlayerTurn();
            }
        }
    }

    void PlayerTurn()
    {
        dialogueText.text = "Choose an action:";
        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);
    }

    void OnVictory()
    {
        dialogueText.text = "Enemy defeated!";
        // Apply victory in GameManager: +levels and stat increases
        GameManager.Instance.ApplyVictory(); // default +levelsPerVictory (5) and +5 damage/maxHP per design

        // Update local playerUnit to match GameManager persist values
        playerUnit.unitLevel = GameManager.Instance.playerLevel;
        playerUnit.damage = GameManager.Instance.damage;
        playerUnit.maxHP = GameManager.Instance.maxHP;
        // currentHP remains as-is (do not modify)

        playerHUD.SetHUD(playerUnit);

        // If this enemy was boss (if prefab had Enemy.isBoss or last prefab), treat as final win
        Enemy enemyScript = currentEnemyGO.GetComponent<Enemy>();
        bool defeatedBoss = enemyScript != null && enemyScript.isBoss;

        // Destroy enemy object
        Destroy(currentEnemyGO);

        // check upgrade trigger
        if (GameManager.Instance.ShouldTriggerUpgrade())
        {
            // show upgrade panel (pauses flow until a button pressed)
            if (upgradePanel != null)
            {
                upgradePanel.Show();
            }
        }

        if (defeatedBoss)
        {
            // Final win. You can show any win UI. For now just show text and stop.
            dialogueText.text = "You defeated the BOSS! You win!";
            // Optionally: freeze the battle system or show victory UI
            state = BattleState.WON;
            return;
        }

        // Spawn next enemy after small delay
        StartCoroutine(SpawnNextAfterDelay(1.2f));
    }

    IEnumerator SpawnNextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        state = BattleState.START;
        StartCoroutine(SetupBattle());
    }

    void OnPlayerDefeated()
    {
        dialogueText.text = "You were defeated...";
        // Apply death penalties from GameManager
        GameManager.Instance.ApplyDeathPenalty();

        // Sync to local playerUnit
        playerUnit.unitLevel = GameManager.Instance.playerLevel;
        playerUnit.damage = GameManager.Instance.damage;
        playerUnit.maxHP = GameManager.Instance.maxHP;
        playerUnit.currentHP = GameManager.Instance.currentHP;

        // Update HUD
        playerHUD.SetHUD(playerUnit);

        // After short delay, respawn next enemy (do not reset run)
        StartCoroutine(RespawnAfterDeath(1.5f));
    }

    IEnumerator RespawnAfterDeath(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Destroy previous enemy and spawn next
        if (currentEnemyGO != null)
            Destroy(currentEnemyGO);

        state = BattleState.START;
        StartCoroutine(SetupBattle());
    }

    // UI button hooks
    public void OnAttackButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        StartCoroutine(PlayerAttack());
    }

    public void OnHealButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        StartCoroutine(PlayerHeal());
    }

    public void OnSpecialButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        StartCoroutine(PlayerSpecial());
    }
}
