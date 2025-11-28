using UnityEngine;

public class rmm : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    SceneController sc;
    void Start()
    {
        sc = GetComponent<SceneController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            sc.LoadScene("MainMenu");
        }
    }
}
