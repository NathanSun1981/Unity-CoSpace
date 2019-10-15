using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Esri.APP;
using UnityEngine.UI;

public class ButtonDimension : MonoBehaviour {

    // Use this for initialization
    public void Start()
    {

        Button btn = this.GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
    } 

    public void OnClick()
    {
        string text = GetComponentInChildren<Text>().text;
        if (text == "2D")
        {
            GetComponentInChildren<Text>().text = "3D";
        }
        else
        {
            GetComponentInChildren<Text>().text = "2D";
        }
        //GameObject ca = GameObject.Find("Camera");
        //ca.SendMessage("OnChangeDimension", text);
        GameObject map = GameObject.Find("Map");
        map.SendMessage("OnChangeDimension", text);
    }
}
