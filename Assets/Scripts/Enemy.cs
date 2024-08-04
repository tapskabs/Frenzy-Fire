using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int enemyIndex; // Assign this in the Inspector

    void Start()
    {
        if (PlayerPrefs.GetInt("EnemyDefeated_" + enemyIndex, 0) == 1)
        {
            gameObject.SetActive(false); // Disable enemy if defeated
        }
    }
}
