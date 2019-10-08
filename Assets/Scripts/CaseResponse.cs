using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CaseResponse : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Button btn = this.GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
    }

    // Update is called once per frame
    void Update()
    {
     
    }

    public void OnClick()
    {
        GameObject obj = new GameObject("iamge", typeof(Image));
        obj.transform.SetParent(GameObject.Find("Canvas").transform);
        obj.name = this.name;
        obj.tag = "Cases";
        obj.GetComponent<Image>().sprite = this.GetComponent<Image>().sprite;
        obj.transform.position = this.transform.position;
        //obj.transform.position = new Vector3 (this.transform.position.x, this.transform.position.y + 0.1f);
        obj.transform.localScale = this.transform.localScale;
        obj.transform.rotation = this.transform.rotation;
        obj.AddComponent<UI2DanchoredPosition>().tag = this.transform.parent.tag;
        obj.AddComponent<Rigidbody>().useGravity = false;
        obj.AddComponent<BoxCollider>().size = new Vector3 (this.GetComponent<RectTransform>().sizeDelta.x, this.GetComponent<RectTransform>().sizeDelta.y, 1f);

    }
 
}
