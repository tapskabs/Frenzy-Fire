using UnityEngine;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    public GameObject panelRoot;
    public Text titleText;
    public Text optionAtext; // e.g. "+10 Max HP"
    public Text optionBtext; // e.g. "+5 Damage"

    // amounts are configurable in inspector or via code
    public int healthAmount = 10;
    public int damageAmount = 5;

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Show()
    {
        if (panelRoot != null)
        {
            optionAtext.text = $"+{healthAmount} Max HP";
            optionBtext.text = $"+{damageAmount} Damage";
            panelRoot.SetActive(true);
            // pause logic if required (disable buttons) should be handled by caller if needed
        }
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // Hook these to the UI buttons:
    public void OnChooseHealth()
    {
        GameManager.Instance.ApplyUpgrade_Health(healthAmount);
        Hide();
    }

    public void OnChooseDamage()
    {
        GameManager.Instance.ApplyUpgrade_Damage(damageAmount);
        Hide();
    }
}
