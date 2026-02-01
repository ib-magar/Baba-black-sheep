using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class endScene : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
    }
}
