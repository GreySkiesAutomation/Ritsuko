using UnityEngine;

public class TestInputBox : MonoBehaviour
{
    [SerializeField] private GameObject _toggleTarget;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            if(_toggleTarget != null)
            {
                _toggleTarget.SetActive(!_toggleTarget.activeSelf);
            }
        }
    }
}
