using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UI2DanchoredPosition : MonoBehaviour, IPointerEnterHandler
{
    private Vector3 prePositon;
    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector2 pos;
    private Camera _camera;
    private RectTransform canvasRectTransform;
    public Touch touch;
    private DateTime m_datetime;
    public string tag;
    private Touch currentTouch;
    void Start()
    {
        rectTransform = transform as RectTransform;
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        _camera = canvas.GetComponent<Camera>();
        canvasRectTransform = canvas.transform as RectTransform;
        prePositon = transform.position;
        m_datetime = DateTime.Now;
        //Debug.Log(canvas.renderMode);

        Button btn = this.GetComponent<Button>();
        btn.onClick.AddListener(() => OnClick(Input.mousePosition));

    }
    void Update()
    {
        FollowFingerMove();
    }

    public void OnClick(Vector2 touchPosition)
    {

        Debug.Log(EventSystem.current.currentSelectedGameObject.name);

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var touches = Input.touches.Where(i => i.position == eventData.position).ToList();
        currentTouch = touches[0];
    }


    public void FollowFingerMove()
    {


        DateTime dTimeNow = DateTime.Now;
        TimeSpan ts = dTimeNow.Subtract(m_datetime);
        float tsf = float.Parse(ts.TotalSeconds.ToString());

        /*
        if (currentTouch.phase == TouchPhase.Moved)
        {
            if (RenderMode.ScreenSpaceCamera == canvas.renderMode)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, currentTouch.position, canvas.worldCamera, out pos);
            }
            else if (RenderMode.ScreenSpaceOverlay == canvas.renderMode)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, currentTouch.position, _camera, out pos);
            }
            else
            {
                Debug.Log("Please choose right Camera RenderMode!");
            }
            rectTransform.anchoredPosition = pos;
        }
        */


        
        if (Input.touchCount > 0)
        {

            Debug.Log("touchcount  = " + Input.touchCount);
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];

                if (EventSystem.current.currentSelectedGameObject == this.gameObject)
                {

                    Debug.Log(this.gameObject.name + "FingerID = " + Input.touches[i].fingerId);
                    m_datetime = DateTime.Now;
                    //worldCamera:1.screenSpace-Camera 
                    //canvas.GetComponent<Camera>() 1.ScreenSpace -Overlay 
                    if (RenderMode.ScreenSpaceCamera == canvas.renderMode)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, touch.position, canvas.worldCamera, out pos);
                    }
                    else if (RenderMode.ScreenSpaceOverlay == canvas.renderMode)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, touch.position, _camera, out pos);
                    }
                    else
                    {
                        Debug.Log("Please choose right Camera RenderMode!");
                    }
                    rectTransform.anchoredPosition = pos;
                }
                
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo))
                {
                    GameObject gameObj = hitInfo.collider.gameObject;
                    if (gameObj == this.gameObject)
                    {
                        m_datetime = DateTime.Now;
                        //worldCamera:1.screenSpace-Camera 
                        //canvas.GetComponent<Camera>() 1.ScreenSpace -Overlay 
                        if (RenderMode.ScreenSpaceCamera == canvas.renderMode)
                        {
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, touch.position, canvas.worldCamera, out pos);
                        }
                        else if (RenderMode.ScreenSpaceOverlay == canvas.renderMode)
                        {
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, touch.position, _camera, out pos);
                        }
                        else
                        {
                            Debug.Log("Please choose right Camera RenderMode!");
                        }
                        rectTransform.anchoredPosition = pos;
                        //Debug.Log(touch.position);
                    }                                     
                }
                
                
            }
        }
        else
        {
            if (Vector3.Distance(transform.position, prePositon) > 0.04f && this.gameObject.tag == "Cases")
            {
                GameObject instance = Instantiate(Resources.Load("SKPFiles/" + this.gameObject.name, typeof(GameObject))) as GameObject;
                instance.tag = tag;
                //instance.AddComponent<CaseControl>();
                instance.AddComponent<RectTransform>();
                instance.name = this.gameObject.name;
                //instance.transform.SetParent(GameObject.Find("MapCanvas").transform);
                instance.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);               

                

                Vector3 screenPos = new Vector3();

                if (RenderMode.ScreenSpaceCamera == canvas.renderMode)
                {
                    screenPos = canvas.worldCamera.WorldToScreenPoint(transform.position);
                }
                else if (RenderMode.ScreenSpaceOverlay == canvas.renderMode)
                {
                    screenPos = _camera.WorldToScreenPoint(transform.position);
                }
                else
                {
                    Debug.Log("Please choose right Camera RenderMode!");
                }

                Vector2 screenPosition = new Vector2(screenPos.x, screenPos.y);           

                RectTransform instanceRect = instance.transform as RectTransform;

                Canvas mapcanvas = GameObject.Find("MapCanvas").GetComponent<Canvas>();
                Camera _mapCamera = mapcanvas.GetComponent<Camera>();
                RectTransform mapcanvasRectTransform = mapcanvas.transform as RectTransform;

                
                if (RenderMode.ScreenSpaceCamera == mapcanvas.renderMode)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(mapcanvasRectTransform, screenPosition, mapcanvas.worldCamera, out pos);
                }
                else if (RenderMode.ScreenSpaceOverlay == mapcanvas.renderMode)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(mapcanvasRectTransform, screenPosition, _mapCamera, out pos);
                }
                else if (RenderMode.WorldSpace == mapcanvas.renderMode)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(mapcanvasRectTransform, screenPosition, Camera.main, out pos);
                }
                else
                {
                    Debug.Log("Please choose right Camera RenderMode!");
                }
                instanceRect.anchoredPosition = pos;
                

                Destroy(this.gameObject);

            }
            
            else if (tsf > 2)
            {              
                Destroy(this.gameObject);

            }
            
        }
        

    }

    Transform FindUpParent(Transform zi)
    {
        if (zi.parent == null)
            return zi;
        else
            return FindUpParent(zi.parent);
    }


}
