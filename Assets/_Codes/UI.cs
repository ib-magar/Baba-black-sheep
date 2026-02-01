using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    public int offset;

    public TextMeshProUGUI text;

    private void Start()
    {
        text.text = $"LEVEL {SceneManager.GetActiveScene().buildIndex +offset}";
    }
}
