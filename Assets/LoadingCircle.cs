using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//from https://www.salusgames.com/2017/01/08/circle-loading-animation-in-unity3d/
public class LoadingCircle : MonoBehaviour
{
    private RectTransform rectComponent;
    private float rotateSpeed = 200f;

    private void Start()
    {
        rectComponent = GetComponent<RectTransform>();
    }

    private void Update()
    {
        rectComponent.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}