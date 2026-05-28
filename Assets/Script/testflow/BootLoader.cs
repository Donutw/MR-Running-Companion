using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    void Start()
    {
        // 确保你的菜单场景名字是 "Menu"，如果不是请替换
        SceneManager.LoadScene("Menu_Scene"); 
    }
}