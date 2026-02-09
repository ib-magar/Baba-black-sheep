using UnityEngine;
using UnityEngine.SceneManagement;

public class external_conrols : MonoBehaviour
{
    
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
}
