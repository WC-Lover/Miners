using System;
using TMPro;
using UnityEngine;

public class PlayerHolyResourceDataUI : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private TextMeshProUGUI holyResourceGatheredText;
    private float holyResourceWeight = 200f;

    public void SetPlayerHolyResourceData(PlayerHolyResourceData phrd)
    {
        playerName.text = phrd.playerName.ToString();
        holyResourceGatheredText.text = Mathf.Ceil(phrd.holyResourceGathered / holyResourceWeight).ToString();
    }
}
