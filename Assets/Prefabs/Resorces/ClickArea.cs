using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClickArea : MonoBehaviour
{
    public float alphaThreshold = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        this.GetComponent<Image>().alphaHitTestMinimumThreshold = alphaThreshold;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
