using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActivateDeactivateTxt : MonoBehaviour
{
    private Text text;
    private bool state;
    // Start is called before the first frame update
    void Start()
    {
        this.GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void activate()
    {
        this.text.color = new Color(205.0f/255.0f,205.0f/255.0f,205.0f/255.0f);
    }

    public void deactivate()
    {
        this.text.color = new Color(45.0f/255.0f,45.0f/255.0f,45.0f/255.0f);
    }
}
