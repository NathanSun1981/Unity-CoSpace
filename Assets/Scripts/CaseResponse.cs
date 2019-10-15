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
        btn.onClick.AddListener(() => OnClick(Input.mousePosition));
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnClick(Vector2 touchPosition)
    {
        GameObject obj = new GameObject("iamge", typeof(Button));
        obj.transform.SetParent(GameObject.Find("Canvas").transform);
        obj.name = this.name;
        obj.tag = "Cases";
        obj.AddComponent<Image>().sprite = this.GetComponent<Image>().sprite;
        obj.AddComponent<BoxCollider>().size = new Vector3(this.GetComponent<RectTransform>().sizeDelta.x, this.GetComponent<RectTransform>().sizeDelta.y, 1f);
        obj.transform.localScale = this.transform.localScale;
        obj.transform.rotation = this.transform.rotation;
        //GameObject.Find("Map").GetComponent<MapLoading>().map.CreateMarker<MarkerBehaviour>("test marker - 1 place St Nizier, Lyon", 
        //new double[2] { GameObject.Find("Map").GetComponent<MapLoading>()._place.Location.Longitude, GameObject.Find("Map").GetComponent<MapLoading>()._place.Location.Latitude }, obj);    
        obj.transform.position = this.transform.position;
        obj.AddComponent<UI2DanchoredPosition>().tag = this.transform.parent.tag;
        //obj.AddComponent<Rigidbody>().useGravity = false;
    }
}
