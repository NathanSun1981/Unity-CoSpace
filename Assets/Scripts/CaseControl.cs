using System.Collections.Generic;
using UnityEngine;
using Esri.APP;
using Esri.PrototypeLab.HoloLens.Unity;
using System;
using UnityEngine.UI;
using System.IO;
using Newtonsoft.Json.Linq;

public class CaseControl : MonoBehaviour
{
    private int PreMapLevel;
    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector2 pos;
    private Camera _camera;
    private RectTransform canvasRectTransform;
    public Touch touch;
    private Coordinate coordinate;
    private DateTime m_datetime;
    private Vector3 initScale;


    public enum ControlCommand { Rotate, Move, Zoom, Delete, Duplicate, information, None };

    public ControlCommand CaseControlCommand = ControlCommand.None;

    void Start()
    {
        rectTransform = transform as RectTransform;
        canvas = GameObject.Find("MapCanvas").GetComponent<Canvas>();
        _camera = canvas.GetComponent<Camera>();
        canvasRectTransform = canvas.transform as RectTransform;
        coordinate = GetCoordinateFromPosition(transform.localPosition);
        PreMapLevel = GameObject.Find("Map").GetComponent<MapLoading>()._place.Level;
        m_datetime = DateTime.Now;
        initScale = transform.localScale = new Vector3(1f / Mathf.Pow(2, (21 - GameObject.Find("Map").GetComponent<MapLoading>()._place.Level)), 1f / Mathf.Pow(2, (21 - GameObject.Find("Map").GetComponent<MapLoading>()._place.Level)), 1f / Mathf.Pow(2, (21 - GameObject.Find("Map").GetComponent<MapLoading>()._place.Level)));

        //Debug.Log(canvas.renderMode);

        /*

        Transform[] grandFa;

        grandFa = GetComponentsInChildren<Transform>();

        foreach (Transform child in grandFa)
        {
            //print(child.name);
            if (child.gameObject.GetComponent<MeshCollider>() != null)
            {
                child.gameObject.GetComponent<MeshCollider>().convex = true;
                child.gameObject.AddComponent<DetectCollision>();
            }
        }
        */

    }
    void Update()
    {

        if (GameObject.Find("Map").GetComponent<MapLoading>().currentDimension == "2D" && GameObject.Find("Map").GetComponent<MapLoading>().currentView == "scene")
        {
            DetectTouchAndGenerateMenu();
            switch (CaseControlCommand)
            {
                case ControlCommand.Move:
                    FollowFingerMove();
                    break;
                case ControlCommand.Rotate:
                    FollowFingerRotate();
                    break;
                case ControlCommand.Zoom:
                    FollowFingerZoom();
                    break;
                case ControlCommand.Delete:
                    break;
                case ControlCommand.information:
                    break;
                case ControlCommand.Duplicate:
                    break;
                default:
                    break;
            }

        }
        else
        {
            if (transform.Find("Menu") != null)
            {
                Destroy(transform.Find("Menu").gameObject);
            }
            if (transform.Find("ScrollInfoControl") != null)
            {
                Destroy(transform.Find("ScrollInfoControl").gameObject);
            }
        }

        if (transform.Find("Menu") != null)
        {
            transform.Find("Menu").localScale *= Mathf.Pow(2, (21 - GameObject.Find("Map").GetComponent<MapLoading>()._place.Level)) / Mathf.Pow(2, (21 - PreMapLevel));
        }

        if (GameObject.Find("Map").GetComponent<MapLoading>()._place.Level != PreMapLevel)
        {
            transform.localScale *= Mathf.Pow(2, (21 - PreMapLevel)) / Mathf.Pow(2, (21 - GameObject.Find("Map").GetComponent<MapLoading>()._place.Level));
            PreMapLevel = GameObject.Find("Map").GetComponent<MapLoading>()._place.Level;
            //Debug.Log(1f / Mathf.Pow(2, (21 - GameObject.Find("Map").GetComponent<MapLoading>()._place.Level)));

        }

        //re-caculate the positon according to the coordinates.
        transform.localPosition = GetPositionFromCoordinates(coordinate);

        if (transform.localPosition.x > canvasRectTransform.sizeDelta.x / 2 || transform.localPosition.x < -canvasRectTransform.sizeDelta.x / 2 || transform.localPosition.y > canvasRectTransform.sizeDelta.x / 2 || transform.localPosition.y < -canvasRectTransform.sizeDelta.x / 2)
        {
            this.gameObject.layer = LayerMask.NameToLayer("Case");
            foreach (Transform tran in GetComponentsInChildren<Transform>())
            {
                tran.gameObject.layer = LayerMask.NameToLayer("Case");
            }
        }
        else
        {
            this.gameObject.layer = LayerMask.NameToLayer("Default");
            foreach (Transform tran in GetComponentsInChildren<Transform>())
            {
                tran.gameObject.layer = LayerMask.NameToLayer("Default");
            }

        }
    }

    public void CaseCommand(string cc)
    {
        switch (cc)
        {
            case "Rotate":
                CaseControlCommand = ControlCommand.Rotate;
                break;
            case "Zoom":
                CaseControlCommand = ControlCommand.Zoom;
                break;
            case "Move":
                CaseControlCommand = ControlCommand.Move;
                break;
            case "Delete":
                CaseControlCommand = ControlCommand.Delete;
                Destroy(this.gameObject);
                break;
            case "Info":
                CaseControlCommand = ControlCommand.information;
                if (transform.Find("ScrollInfoControl") == null)
                {
                    GameObject scrollInfoControl = (GameObject)Instantiate(Resources.Load("ScrollInfoControl"));
                    scrollInfoControl.transform.SetParent(this.transform);
                    scrollInfoControl.transform.name = "ScrollInfoControl";
                    RectTransform rt = scrollInfoControl.GetComponent<RectTransform>();


                    switch (this.gameObject.tag)
                    {
                        case "Down":
                            rt.localPosition = new Vector3(0, 0, 0);
                            rt.offsetMax = new Vector2(0, 50f);
                            rt.offsetMin = new Vector2(150f, 0);
                            break;
                        case "Left":
                            rt.localPosition = new Vector3(0, 0, -100);
                            rt.offsetMax = new Vector2(0, 0);
                            rt.offsetMin = new Vector2(0, 0);
                            break;
                        case "Right":
                            rt.localPosition = new Vector3(0, 0, 100);
                            rt.offsetMax = new Vector2(0, 0);
                            rt.offsetMin = new Vector2(0, 0);
                            break;
                        default:
                            rt.localPosition = new Vector3(0, 0, 0);
                            rt.offsetMax = new Vector2(0, 50f);
                            rt.offsetMin = new Vector2(150f, 0);
                            break;
                    }

                    scrollInfoControl.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    scrollInfoControl.transform.eulerAngles = transform.Find("Menu").transform.eulerAngles;
                    scrollInfoControl.transform.Find("Image").Find("Text").GetComponent<Text>().text = GetCaseInfo(this.gameObject.name);

                }

                break;
            case "Duplicate":
                CaseControlCommand = ControlCommand.Duplicate;
                break;
            default:
                CaseControlCommand = ControlCommand.None;
                break;

        }
    }
    string GetCaseInfo(string name)
    {
        string result = "no info";
        string filePath = Path.Combine(Application.streamingAssetsPath, "cases.json");

        if (File.Exists(filePath))
        {
            string dataAsJson;
            dataAsJson = File.ReadAllText(filePath);
            var cases = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(dataAsJson);
            foreach (JObject dbcase in cases)
            {
                if (dbcase.GetValue("_CaseNumber").ToString().Equals(name))
                {
                    return dbcase.ToString();
                }
            }

        }
        else
        {
            Debug.LogError("Cannot load task data!");
        }

        return result;

    }

    public void DetectTouchAndGenerateMenu()
    {

        DateTime dTimeNow = DateTime.Now;
        TimeSpan ts = dTimeNow.Subtract(m_datetime);
        float tsf = float.Parse(ts.TotalSeconds.ToString());

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];

                //Debug.Log(i.ToString() + ":" + touch.phase.ToString());

                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo))
                {
                    GameObject gameObj = hitInfo.collider.gameObject;
                    if (gameObj == this.gameObject || (gameObj.transform.parent != null && gameObj.transform.parent.gameObject == this.gameObject) || (gameObj.transform.parent.transform.parent != null && gameObj.transform.parent.transform.parent.gameObject == this.gameObject))
                    {
                        m_datetime = DateTime.Now;
                        //generate a menu 
                        if (touch.phase == TouchPhase.Began)
                        {
                            /*
                            foreach (var t in GetComponentsInChildren<Transform>())
                            {
                                if (t.name == "Menu" && CaseControlCommand == ControlCommand.None)
                                {
                                    CaseControlCommand = ControlCommand.None;
                                    Destroy(transform.Find("Menu").gameObject);
                                    return;
                                }
                            }
                            */
                            if (transform.Find("Menu") == null)
                            {
                                GameObject menu = (GameObject)Instantiate(Resources.Load("Menu"));
                                menu.name = "Menu";
                                menu.transform.SetParent(this.transform);
                                menu.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f) * Mathf.Pow(2, (21 - GameObject.Find("Map").GetComponent<MapLoading>()._place.Level));
                                switch (this.gameObject.tag)
                                {
                                    case "Down":
                                        menu.transform.localPosition = this.transform.localPosition + new Vector3(-50f, 100f, 0f);
                                        menu.transform.eulerAngles = new Vector3(90, 0, 180);
                                        break;
                                    case "Left":
                                        menu.transform.localPosition = this.transform.localPosition + new Vector3(0f, 100f, 50f);
                                        menu.transform.eulerAngles = new Vector3(90, 90, 180);
                                        break;
                                    case "Right":
                                        menu.transform.localPosition = this.transform.localPosition + new Vector3(0f, 100f, -50f);
                                        menu.transform.eulerAngles = new Vector3(90, -90, 180);
                                        break;
                                    default:
                                        menu.transform.localPosition = this.transform.localPosition + new Vector3(-50f, 100f, 0f);
                                        menu.transform.eulerAngles = new Vector3();
                                        break;
                                }


                            }


                        }
                    }
                }
            }
        }
        else if (tsf > 2)
        {
            if (transform.Find("Menu") != null)
            {
                Destroy(transform.Find("Menu").gameObject);
            }
        }
    }
    public void FollowFingerMove()
    {

        //finger should touch the object, later will detect the closest finger and following
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];

                //Debug.Log(i.ToString() + ":" + touch.phase.ToString());

                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo))
                {
                    GameObject gameObj = hitInfo.collider.gameObject;
                    if (gameObj == this.gameObject || (gameObj.transform.parent != null && gameObj.transform.parent.gameObject == this.gameObject) || (gameObj.transform.parent.transform.parent != null && gameObj.transform.parent.transform.parent.gameObject == this.gameObject))
                    {
                        //worldCamera:1.screenSpace-CameraÂ 
                        //canvas.GetComponent<Camera>() 1.ScreenSpace -OverlayÂ 
                        if (RenderMode.ScreenSpaceCamera == canvas.renderMode)
                        {
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, touch.position, canvas.worldCamera, out pos);
                        }
                        else if (RenderMode.ScreenSpaceOverlay == canvas.renderMode)
                        {
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, touch.position, _camera, out pos);
                        }
                        else if (RenderMode.WorldSpace == canvas.renderMode)
                        {
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, touch.position, Camera.main, out pos);
                        }
                        else
                        {
                            Debug.Log("Please choose right Camera RenderMode!");
                        }
                        rectTransform.anchoredPosition = pos;

                        coordinate = GetCoordinateFromPosition(transform.localPosition);

                    }
                }

            }
        }
    }

    public void FollowFingerRotate()
    {
        if (DetectClosestFingerTouch(2))
            this.gameObject.transform.RotateAround(Vector3.up, -DetectClosestTouchMovement.turnAngleDelta * 0.2f * Time.deltaTime);
    }

    public void FollowFingerZoom()
    {
        if (DetectClosestFingerTouch(2))
        {
            //Debug.Log(1 + DetectClosestTouchMovement.pinchAmount / 50);
            this.gameObject.transform.localScale *= (1 + DetectClosestTouchMovement.pinchAmount / 50);

            this.gameObject.transform.localScale = new Vector3(Mathf.Clamp(this.gameObject.transform.localScale.x, initScale.x / 1.2f, initScale.x * 1.2f),
                Mathf.Clamp(this.gameObject.transform.localScale.y, initScale.y / 1.2f, initScale.y * 1.2f),
                Mathf.Clamp(this.gameObject.transform.localScale.z, initScale.z / 1.2f, initScale.z * 1.2f));
            /*
            if (transform.Find("Menu") != null)
            {

                transform.Find("Menu").localScale /= (1 + DetectClosestTouchMovement.pinchAmount / 50);
            }
            */

        }
    }

    public bool DetectClosestFingerTouch(int FingerNum)
    {
        //DetectClosestFingerTouch in a certain distance
        Touch[] allTouches = Input.touches;
        Touch[] closestTouchs = new Touch[FingerNum];

        if (Input.touchCount >= FingerNum)
        {
            float[] distance = new float[Input.touchCount];
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];

                //Debug.Log(string.Format("detected the {0} finger", i));
                //worldCamera:1.screenSpace-CameraÂ 
                //canvas.GetComponent<Camera>() 1.ScreenSpace -OverlayÂ 
                if (RenderMode.ScreenSpaceCamera == canvas.renderMode)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, touch.position, canvas.worldCamera, out pos);
                }
                else if (RenderMode.ScreenSpaceOverlay == canvas.renderMode)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, touch.position, _camera, out pos);
                }
                else if (RenderMode.WorldSpace == canvas.renderMode)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, touch.position, Camera.main, out pos);
                }
                else
                {
                    Debug.Log("Please choose right Camera RenderMode!");
                }

                distance[i] = Vector2.Distance(rectTransform.anchoredPosition, pos);
                //Debug.Log(string.Format("distance of the {0} finger from selected cases {2} is {1}", i + 1, distance[i], this.gameObject.name));

            }

            if (FingerNum == 2)
            {
                int[] indexs = new int[FingerNum];
                float[] mindistance = FindMinumFromArray(distance, FingerNum, out indexs);
                for (int i = 0; i < FingerNum; i++)
                {
                    //Debug.Log(mindistance[i]);
                    if (mindistance[i] > 2f)
                    {
                        return false;
                    }

                    closestTouchs[i] = Input.touches[indexs[i]];
                }

                DetectClosestTouchMovement.Calculate(closestTouchs);
                return true;
            }
        }
        return false;
    }

    public float[] FindMinumFromArray(float[] ins, int k, out int[] indexsofClosestTouches)
    {
        float[] ks = new float[k];
        indexsofClosestTouches = new int[k];

        if (ins.Length < k)
        {
            for (int i = 0; i < ins.Length; i++)
            {
                indexsofClosestTouches[i] = i;
            }
            return ins;
        }


        for (int i = 0; i < k; i++)
        {
            ks[i] = ins[i];
            indexsofClosestTouches[i] = i;
        }
        for (int i = k; i < ins.Length; i++)
        {
            if (getMax(ks, ref indexsofClosestTouches) > ins[i])
            {
                ks[0] = ins[i];
                indexsofClosestTouches[0] = i;
            }
        }
        return ks;
    }
    public static float getMax(float[] arr, ref int[] indexs)
    {
        int radix = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[radix] < arr[i])
            {
                float temp = arr[radix];
                arr[radix] = arr[i];
                arr[i] = temp;

                int tempindex = indexs[radix];
                indexs[radix] = indexs[i];
                indexs[i] = tempindex;
            }
        }
        return arr[radix];
    }


    public Coordinate GetCoordinateFromPosition(Vector3 position)
    {
        // Get UL and LR coordinates
        MapLoading maploading = GameObject.Find("Map").GetComponent<MapLoading>();
        var tileUL = maploading._place.Location.ToTile(maploading._place.Level);
        var tileLR = new Tile()
        {
            Zoom = tileUL.Zoom,
            X = tileUL.X + maploading.CHILDREN_LEVEL * 2,
            Y = tileUL.Y + maploading.CHILDREN_LEVEL * 2
        };
        var coordUL = tileUL.UpperLeft(maploading.CHILDREN_LEVEL);
        var coordLR = tileLR.UpperLeft(maploading.CHILDREN_LEVEL);

        // Get tapped location relative to lower left.
        var location = position - GameObject.Find("Map").transform.position;

        var longitude = coordUL.Longitude + (coordLR.Longitude - coordUL.Longitude) * ((location.x + canvasRectTransform.sizeDelta.x / 2) / canvasRectTransform.sizeDelta.x);
        var lattitude = coordLR.Latitude + (coordUL.Latitude - coordLR.Latitude) * ((location.y + canvasRectTransform.sizeDelta.y / 2) / canvasRectTransform.sizeDelta.y);

        var coordinate = new Coordinate()
        {
            Longitude = longitude,
            Latitude = lattitude
        };

        //Debug.Log(coordinate.Longitude);

        //Debug.Log(coordinate.Latitude);
        return coordinate;
    }

    public Vector3 GetPositionFromCoordinates(Coordinate coordinate)
    {

        MapLoading maploading = GameObject.Find("Map").GetComponent<MapLoading>();
        var tileUL = maploading._place.Location.ToTile(maploading._place.Level);
        var tileLR = new Tile()
        {
            Zoom = tileUL.Zoom,
            X = tileUL.X + maploading.CHILDREN_LEVEL * 2,
            Y = tileUL.Y + maploading.CHILDREN_LEVEL * 2
        };
        var coordUL = tileUL.UpperLeft(maploading.CHILDREN_LEVEL);
        var coordLR = tileLR.UpperLeft(maploading.CHILDREN_LEVEL);

        // Get tapped location relative to lower left.
        GameObject map = GameObject.Find("Map");

        Vector3 locationonMap = new Vector3();

        locationonMap.x = (float)((coordinate.Longitude - coordUL.Longitude) / (coordLR.Longitude - coordUL.Longitude) * canvasRectTransform.sizeDelta.x - canvasRectTransform.sizeDelta.x / 2);
        locationonMap.y = (float)((coordinate.Latitude - coordLR.Latitude) / (coordUL.Latitude - coordLR.Latitude) * canvasRectTransform.sizeDelta.y - canvasRectTransform.sizeDelta.y / 2);
        locationonMap.z = -0.1f;

        return (locationonMap + map.transform.position);

    }

}