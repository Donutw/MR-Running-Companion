using UnityEngine;
using UnityEngine.SceneManagement; // 必须引用这个才能换场景

public class SceneSwitcher : MonoBehaviour {
    
    // 给测试 A 按钮用
    public void GoToTestA() {
        // SaveCurrentData(); 
        SceneManager.LoadScene("TestA_Scene"); // 填你场景的精确名字
    }

    // 给测试 B 按钮用
    public void GoToTestB() {
        SceneManager.LoadScene("TestB_Scene");
    }

    // 给返回主菜单按钮用
    public void GoToMenu() {
        SceneManager.LoadScene("Menu_Scene");
    }
}