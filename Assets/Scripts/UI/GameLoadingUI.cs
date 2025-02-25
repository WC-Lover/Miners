using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class GameLoadingUI : MonoBehaviour
{
    public TextMeshProUGUI loadingText;
    private string baseText = "LOADING";
    private int dots = 0;
    private StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        GameManager.Instance.OnGameReady += GameConnectionManager_OnGameReady;
    }

    void Start()
    {
        StartCoroutine(AnimateLoadingText());
    }

    private void GameConnectionManager_OnGameReady(object sender, EventArgs e)
    {
        GameManager.Instance.OnGameReady -= GameConnectionManager_OnGameReady;
        StopCoroutine(AnimateLoadingText());
        gameObject.SetActive(false);
    }

    IEnumerator AnimateLoadingText()
    {
        while (true)
        {
            sb.Clear();
            sb.Append(baseText);
            for (int i = 0; i < dots; i++)
            {
                sb.Append(".");
            }
            loadingText.text = sb.ToString();

            dots = (dots + 1) % 4;

            yield return new WaitForSeconds(1f);
        }
    }
}
