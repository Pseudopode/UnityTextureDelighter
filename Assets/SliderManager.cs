using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.Events;

public class SliderManager : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler, IDragHandler
{
    // Set those in the inspector or via AddListener exactly the same as onClick of a button
    public UnityEvent onPointerEnter;
    public UnityEvent onPointerExit;
    public UnityEvent onDrag;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // evtl put some general button fucntionality here
        
        onPointerEnter.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // evtl put some general button fucntionalit here
    
        onPointerExit.Invoke();
    }   

       public void OnDrag(PointerEventData eventData)
    {
        // evtl put some general button fucntionalit here
    
        onDrag.Invoke();
    }  
}
