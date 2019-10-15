/*
    Copyright 2016 Esri

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.

    You may obtain a copy of the License at
    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Networking;
using Esri.PrototypeLab.HoloLens.Unity;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Esri.APP
{
    public class SlippyMapLoading : MonoBehaviour
    {

        private bool _isMapLoaded = false;
        private bool _isFirstTimeLoading = true;
        private bool _NeedReloadMap = false;
        private bool _isBuildingMap = false;
        public Place _place;
        private Place[] places;
        private readonly string queryURL = "http://127.0.0.1:8080/querydata.php";
        private string mapName;
        private string reloadmapname;
        private string m_xml = "initial";
        private DateTime m_datetime;

        public float SIZE = 1f;
        public int CHILDREN_LEVEL = 2; // 1 = Four child image tiles, 2 = Sixteen child images.
        public string currentDimension = "2D";
        public string currentStyle = "satellite";
        public string currentView = "scene";
        public float perspectiveZoomSpeed = 0.5f;        // The rate of change of the field of view in perspective mode.
        public float orthoZoomSpeed = 0.5f;
        public Camera mainCamera;

        public bool swiping;
        public float minSwipeDistance;
        public float errorRange = 5f;
        public SwipeDirection direction = SwipeDirection.None;
        public enum SwipeDirection { Right, Left, Up, Down, None }
        private Touch initialTouch;
        private readonly string gameDataFileName = "tasks.json";

        public void Start()
        {
            m_datetime = DateTime.Now;
            currentDimension = "2D";
#if UNITY_WSA  && !UNITY_EDITOR
            WriteConfigrationToStorage();
#endif

            //StartCoroutine(DownloadPlaces(queryURL));
            DownloadPlaces();

        }

        public void Update()
        {

            DateTime dTimeNow = DateTime.Now;
            TimeSpan ts = dTimeNow.Subtract(m_datetime);
            float tsf = float.Parse(ts.TotalSeconds.ToString());
            if (tsf > 3)
            {
                //StartCoroutine(CheckExistmap(queryURL));
                CheckExistmap();
                m_datetime = DateTime.Now;
            }

            DetectonMoveDirection();

        }

        public void LateUpdate()
        {
            if (this._isMapLoaded && this._isFirstTimeLoading)
            {
                for (int i = 0; i < places.Length; i++)
                {
                    if (places[i].Name == "Default")
                    {
                        mapName = places[i].Name;
                        this._place = places[i];
                        this.StartCoroutine(this.AddMap(places[i]));
                    }
                }

                this._isFirstTimeLoading = false;

            }

            //add 1
            if (this._NeedReloadMap && !this._isFirstTimeLoading)
            {
                for (int i = 0; i < places.Length; i++)
                {
                    if (places[i].Name == reloadmapname)
                    {
                        mapName = places[i].Name;
                        this._NeedReloadMap = false;
                        this.StartCoroutine(this.AddMap(places[i]));
                    }
                }
            }


            if (!DetectTouchOnOtherObject())
            {
                float pinchAmount = 0;
                Quaternion desiredRotation = mainCamera.transform.rotation;

                DetectTouchMovement.Calculate();

                if (Mathf.Abs(DetectTouchMovement.pinchDistanceDelta) > 0)
                { // zoom
                    pinchAmount = DetectTouchMovement.pinchDistanceDelta;
                }

                if (Mathf.Abs(DetectTouchMovement.turnAngleDelta) > 0)
                { // rotate
                    Vector3 rotationDeg;
                    rotationDeg = Vector3.forward;
                    rotationDeg.z = -DetectTouchMovement.turnAngleDelta;
                    desiredRotation *= Quaternion.Euler(rotationDeg);
                }

                if (currentView == "scene")
                {

                    if (currentDimension == "3D")
                    {
                        if (mainCamera.orthographic)
                        {
                            // ... change the orthographic size based on the change in distance between the touches.
                            mainCamera.orthographicSize += -pinchAmount * orthoZoomSpeed;

                            // Make sure the orthographic size never drops below zero.
                            mainCamera.orthographicSize = Mathf.Max(mainCamera.orthographicSize, 0.1f);
                        }
                        else
                        {
                            // Otherwise change the field of view based on the change in distance between the touches.
                            mainCamera.fieldOfView += -pinchAmount * perspectiveZoomSpeed;

                            // Clamp the field of view to make sure it's between 0 and 180.
                            mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView, 30f, 130f);
                        }
                        mainCamera.transform.RotateAround(Vector3.zero, Vector3.up, DetectTouchMovement.turnAngleDelta);
                    }
                    else
                    {
                        mainCamera.fieldOfView = 35f;
                    }



                }
                else
                {
                    if (pinchAmount > 0 && !_isBuildingMap)
                    {
                        _isBuildingMap = true;
                        //Debug.Log(pinchAmount);
                        OnClickZoomIn();
                    }
                    //Zoom in
                    if (pinchAmount < 0 && !_isBuildingMap)
                    {
                        _isBuildingMap = true;
                        //Debug.Log(pinchAmount);
                        OnClickZoomOut();
                    }

                    switch (direction)
                    {
                        case SwipeDirection.Left:
                            if (!_isBuildingMap)
                            {
                                _isBuildingMap = true;
                                OnClickMoveLeft();
                            }
                            break;
                        case SwipeDirection.Right:
                            if (!_isBuildingMap)
                            {
                                _isBuildingMap = true;
                                OnClickMoveRight();
                            }
                            break;
                        case SwipeDirection.Up:
                            if (!_isBuildingMap)
                            {
                                _isBuildingMap = true;
                                OnClickMoveUp();
                            }
                            break;
                        case SwipeDirection.Down:
                            if (!_isBuildingMap)
                            {
                                _isBuildingMap = true;
                                OnClickMoveDown();
                            }
                            break;
                    }

                }


            }

        }

        public bool DetectTouchOnOtherObject()
        {
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
                        return true;

                    }

                }
            }

            return false;
        }

        public void DetectonMoveDirection()
        {
            //if one finger is touching the screen
            if (Input.touchCount == 1)
            {
                Touch touch = Input.touches[0];
                if (touch.phase == TouchPhase.Began)
                {
                    initialTouch = touch;
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    var deltaX = touch.position.x - initialTouch.position.x; //greater than 0 is right and less than zero is left
                    var deltaY = touch.position.y - initialTouch.position.y; //greater than 0 is up and less than zero is down
                    var swipeDistance = Mathf.Abs(deltaX) + Mathf.Abs(deltaY);

                    if (swipeDistance > minSwipeDistance && (Mathf.Abs(deltaX) > 0 || Mathf.Abs(deltaY) > 0))
                    {
                        swiping = true;

                        CalculateSwipeDirection(deltaX, deltaY);
                    }
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    initialTouch = new Touch();
                    swiping = false;
                    direction = SwipeDirection.None;
                }
                else if (touch.phase == TouchPhase.Canceled)
                {
                    initialTouch = new Touch();
                    swiping = false;
                    direction = SwipeDirection.None;
                }

            }

        }



        void CalculateSwipeDirection(float deltaX, float deltaY)
        {
            bool isHorizontalSwipe = Mathf.Abs(deltaX) > Mathf.Abs(deltaY);

            // horizontal swipe
            if (isHorizontalSwipe && Mathf.Abs(deltaY) <= errorRange)
            {
                //right
                if (deltaX > 0)
                    direction = SwipeDirection.Left;
                //left
                else if (deltaX < 0)
                    direction = SwipeDirection.Right;
            }
            //vertical swipe
            else if (!isHorizontalSwipe && Mathf.Abs(deltaX) <= errorRange)
            {
                //up
                if (deltaY > 0)
                    direction = SwipeDirection.Down;
                //down
                else if (deltaY < 0)
                    direction = SwipeDirection.Up;
            }
            //diagonal swipe
            else
            {
                swiping = false;
            }
        }


        private IEnumerator AddMap(Place place)
        {
            // Store current place
            this._place = place;

            // Convert lat/long to Google/Bing/AGOL tile.
            var tile = this._place.Location.ToTile(this._place.Level);

            // Get children.
            //var children = tile.GetChildren(CHILDREN_LEVEL);

            var children = tile.GetNeighbor(CHILDREN_LEVEL);

            // Elevation and texture variables.
            Texture2D[] textures = new Texture2D[children.Length];

            yield return null;

            // Retrieve imagery.
            foreach (var child in children)
            {
                this.StartCoroutine(Imagery.GetTexture(child, currentStyle, texture =>
                {
                    textures[Array.IndexOf(children, child)] = texture;

                    if (textures.All(t => t != null))
                    {
                        this.StartCoroutine(this.BuildMap(textures, children));
                    }
                }));
            }

        }

        private IEnumerator BuildMap(Texture2D[] textures, Tile[] children)
        {
            //delete old map tiles
            while (true)
            {
                GameObject[] tcs = GameObject.FindGameObjectsWithTag("Tile");
                if (tcs.Length > 0)
                {
                    foreach (GameObject tc in tcs)
                    {
                        GameObject.Destroy(tc);
                    }
                }
                else
                {
                    break;
                }
                yield return null;
            }
            // Center position of terrain.
            int i = 0;

            var position = this.transform.position;

            // Tiles per side.
            var dimension = (int)Math.Sqrt(textures.Length);

            // Splat maps.
            var splats = new List<SplatPrototype>();
            foreach (var texture in textures)
            {

                GameObject tile = new GameObject();
                tile.transform.parent = this.gameObject.transform;
                tile.tag = "Tile";
                tile.name = children[Array.IndexOf(textures, texture)].Zoom + "/" + children[Array.IndexOf(textures, texture)].X + "/" + children[Array.IndexOf(textures, texture)].Y;

                tile.transform.position = new Vector3(position.x - (i % 4) * 10 + 15, position.y, position.z + (i / 4 + 1) * 10 - 25);
                i++;
                MeshFilter mf = tile.gameObject.AddComponent<MeshFilter>();
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                mf.mesh = go.GetComponent<MeshFilter>().mesh;
                Destroy(go);
                MeshRenderer rd = tile.gameObject.AddComponent<MeshRenderer>();
                rd.material.mainTexture = texture;
                yield return null;
            }

            _isBuildingMap = false;


        }

        private IEnumerator DownloadPlaces(string url)
        {
            string xml;
            int i = 0;
            url += "?action=maplist";
            UnityWebRequest hs_get = UnityWebRequest.Get(url);
            yield return hs_get.SendWebRequest();
            if (hs_get.error != "" && hs_get.error != null)
            {
                Debug.Log(hs_get.error);
                xml = "";
            }
            else
            {
                xml = hs_get.downloadHandler.text;
                if (!xml.Equals(m_xml) || (m_xml.Equals("initial")))
                {
                    m_xml = xml;
                    string[] maps = xml.Split('\n');
                    this.places = new Place[maps.Length - 1];

                    foreach (string map in maps)
                    {
                        if (map.Length > 0)
                        {
                            string[] mapinfo = map.Split('\t');
                            places[i] = new Place();
                            places[i].Name = mapinfo[0];
                            places[i].Location = new Coordinate();
                            places[i].Location.Longitude = float.Parse(mapinfo[1]);
                            places[i].Location.Latitude = float.Parse(mapinfo[2]);
                            places[i].Level = int.Parse(mapinfo[3]);
                            i++;

                        }
                    }
                }
                this._isMapLoaded = true;
            }
        }

        private async void DownloadPlaces()
        {
#if UNITY_WSA && !UNITY_EDITOR
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(gameDataFileName,
                        Windows.Storage.CreationCollisionOption.OpenIfExists);
        string filePath = file.Path;
#else
            string filePath = Path.Combine(Application.streamingAssetsPath, gameDataFileName);
#endif


            //string filePath = Path.Combine(Application.streamingAssetsPath, gameDataFileName);
            int i = 0;

            if (File.Exists(filePath))
            {
                // Read the json from the file into a string
                string dataAsJson = File.ReadAllText(filePath);
                // Pass the json to JsonUtility, and tell it to create a GameData object from it

                JObject obj = JObject.Parse(dataAsJson);

                JArray tasks = (JArray)obj["Tasks"];


                this.places = new Place[tasks.Count];

                foreach (var task in tasks)
                {
                    places[i] = new Place();
                    places[i].Name = task["Name"].ToString();
                    places[i].Location = new Coordinate();
                    string log = task["coordinates"]["Longitude"].ToString();
                    places[i].Location.Longitude = float.Parse(task["coordinates"]["Longitude"].ToString());
                    places[i].Location.Latitude = float.Parse(task["coordinates"]["Latitude"].ToString());
                    places[i].Level = int.Parse(task["Level"].ToString());
                    i++;
                }

                this._isMapLoaded = true;
            }
            else
            {
                Debug.LogError("Cannot load task data!");
            }
        }

        private IEnumerator CheckExistmap(string url)
        {


            url += "?action=maploaded";
            UnityWebRequest hs_get = UnityWebRequest.Get(url);
            yield return hs_get.SendWebRequest();
            if (hs_get.error != "" && hs_get.error != null)
            {
                Debug.Log(hs_get.error);
            }
            else
            {
                string xml = hs_get.downloadHandler.text;
                if (xml != "" && xml != mapName)
                {
                    _NeedReloadMap = true;
                    reloadmapname = xml;
                }
            }


        }

        private async void CheckExistmap()
        {
#if UNITY_WSA && !UNITY_EDITOR
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(gameDataFileName,
                        Windows.Storage.CreationCollisionOption.OpenIfExists);
        string filePath = file.Path;
#else
            string filePath = Path.Combine(Application.streamingAssetsPath, gameDataFileName);
#endif

            //string filePath = Path.Combine(Application.streamingAssetsPath, gameDataFileName);

            if (File.Exists(filePath))
            {
                // Read the json from the file into a string
                string dataAsJson = File.ReadAllText(filePath);
                // Pass the json to JsonUtility, and tell it to create a GameData object from it

                JObject obj = JObject.Parse(dataAsJson);
                // Retrieve the allRoundData property of loadedData
                reloadmapname = obj["loadedmap"].ToString();
                if (reloadmapname != mapName)
                {
                    _NeedReloadMap = true;
                }

            }
            else
            {
                Debug.LogError("Cannot load task data!");
            }
        }


        public void OnClickChangeView(string viewType)
        {
            this.currentView = viewType;
        }

        public void OnChangeDimension(string dimension)
        {
            this.currentDimension = dimension;
        }

        public void OnChangeMapstyle(string mapstyle)
        {
            this.currentStyle = mapstyle;
            this.StartCoroutine(this.AddMap(this._place));

        }

        public void OnClickZoomOut()
        {
            // 缩小 - 图标
            this._place.Level -= 1;
            this.StartCoroutine(this.AddMap(this._place));
        }

        public void OnClickZoomIn()
        {
            //zoom in 放大 + 图标
            this._place.Level += 1;
            this.StartCoroutine(this.AddMap(this._place));
        }


        public void OnClickMoveLeft()
        {
            this._place = GetLocalTileCenterCoordintes("left");
            this.StartCoroutine(this.AddMap(this._place));
        }

        public void OnClickMoveRight()
        {
            this._place = GetLocalTileCenterCoordintes("right");
            this.StartCoroutine(this.AddMap(this._place));
        }

        public void OnClickMoveUp()
        {
            this._place = GetLocalTileCenterCoordintes("up");
            this.StartCoroutine(this.AddMap(this._place));
        }

        public void OnClickMoveDown()
        {
            this._place = GetLocalTileCenterCoordintes("down");
            this.StartCoroutine(this.AddMap(this._place));
        }

        public void OnClickMoveLeftUp()
        {
            this._place = GetLocalTileCenterCoordintes("leftup");
            this.StartCoroutine(this.AddMap(this._place));
        }
        public void OnClickMoveLeftDown()
        {
            this._place = GetLocalTileCenterCoordintes("leftdown");
            this.StartCoroutine(this.AddMap(this._place));
        }
        public void OnClickMoveRightUp()
        {
            this._place = GetLocalTileCenterCoordintes("rightup");
            this.StartCoroutine(this.AddMap(this._place));
        }
        public void OnClickMoveRightDown()
        {
            this._place = GetLocalTileCenterCoordintes("rightdown");
            this.StartCoroutine(this.AddMap(this._place));
        }

        public void OnClickMaintain()
        {
            SceneManager.LoadScene(1);
        }

        public async void OnClickExit()
        {
            /*
#if UNITY_WSA && !UNITY_EDITOR

            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(gameDataFileName,
                        Windows.Storage.CreationCollisionOption.OpenIfExists);
        string filePath = file.Path;
#else
            string filePath = Path.Combine(Application.streamingAssetsPath, gameDataFileName);
#endif

            if (File.Exists(filePath))
            {
                // Read the json from the file into a string

                string dataAsJson = File.ReadAllText(filePath);
                // Pass the json to JsonUtility, and tell it to create a GameData object from it
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(dataAsJson);

                jsonObj["loadedmap"] = "Default";
                string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                //File.WriteAllText(filePath, output);

#if UNITY_WSA && !UNITY_EDITOR

                await Windows.Storage.FileIO.WriteTextAsync(file, output);
#else
                File.WriteAllText(filePath, output);
#endif
*/
            Application.Quit();
        }


        public void OnClickMoveConfirm(Vector3 position)
        {
            this.StartCoroutine(this.ChangeMapCoordinates(position - new Vector3(0f, 0.2f, 0f)));
        }

        private Place GetLocalTileCenterCoordintes(string direction)
        {
            var tileUL = this._place.Location.ToTile(this._place.Level);

            switch (direction)
            {
                case "left":
                    tileUL.X -= 1;
                    break;
                case "right":
                    tileUL.X += 1;
                    break;
                case "down":
                    tileUL.Y += 1;
                    break;
                case "up":
                    tileUL.Y -= 1;
                    break;
                case "leftup":
                    tileUL.X -= 1;
                    tileUL.Y -= 1;
                    break;
                case "leftdown":
                    tileUL.X -= 1;
                    tileUL.Y += 1;
                    break;
                case "rightup":
                    tileUL.X += 1;
                    tileUL.Y -= 1;
                    break;
                case "rightdown":
                    tileUL.X += 1;
                    tileUL.Y += 1;
                    break;
                default:
                    return this._place;
            }

            var tileLR = new Tile()
            {
                Zoom = tileUL.Zoom,
                X = tileUL.X + CHILDREN_LEVEL * 2,
                Y = tileUL.Y + CHILDREN_LEVEL * 2
            };
            var coordUL = tileUL.UpperLeft(CHILDREN_LEVEL);
            var coordLR = tileLR.UpperLeft(CHILDREN_LEVEL);

            // Get tapped location relative to lower left.
            var coordCN = new Coordinate()
            {
                Latitude = coordUL.Latitude + (coordLR.Latitude - coordUL.Latitude) / 4f * 2.5f,
                Longitude = coordUL.Longitude + (coordLR.Longitude - coordUL.Longitude) / 4f * 2.5f,
            };

            this._place.Location = coordCN;
            return this._place;

        }

        private IEnumerator ChangeMapCoordinates(Vector3 position)
        {
            // Get UL and LR coordinates
            var tileUL = this._place.Location.ToTile(this._place.Level);
            var tileLR = new Tile()
            {
                Zoom = tileUL.Zoom,
                X = tileUL.X + CHILDREN_LEVEL * 2,
                Y = tileUL.Y + CHILDREN_LEVEL * 2
            };
            var coordUL = tileUL.UpperLeft(CHILDREN_LEVEL);
            var coordLR = tileLR.UpperLeft(CHILDREN_LEVEL);

            // Get tapped location relative to lower left.
            GameObject terrain = GameObject.Find("terrain");
            var location = position - terrain.transform.position;

            var longitude = coordUL.Longitude + (coordLR.Longitude - coordUL.Longitude) * (location.x / SIZE);
            var lattitude = coordLR.Latitude + (coordUL.Latitude - coordLR.Latitude) * (location.z / SIZE);

            var coordinate = new Coordinate()
            {
                Longitude = longitude,
                Latitude = lattitude
            };
            this._place.Location = coordinate;
            //this._place.Level += 1;
            this.StartCoroutine(this.AddMap(this._place));

            yield return null;
        }

#if UNITY_WSA && !UNITY_EDITOR

        async void WriteConfigrationToStorage()
        {

                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(gameDataFileName,
                        Windows.Storage.CreationCollisionOption.OpenIfExists);

                string filePath = Path.Combine(Application.streamingAssetsPath, gameDataFileName);


                if (File.Exists(filePath) && File.Exists(file.Path))
                {
                    // Read the json from the file into a string

                    string dataAsJson = File.ReadAllText(filePath);
                    // Pass the json to JsonUtility, and tell it to create a GameData object from it
                    await Windows.Storage.FileIO.WriteTextAsync(file, dataAsJson);

                }
                else
                {
                    Debug.LogError("Cannot load task data!");
                }
        }

#endif
    }

}
