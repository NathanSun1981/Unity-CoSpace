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
using UnitySlippyMap.Map;

using UnitySlippyMap.Layers;
using UnityEngine.EventSystems;

namespace Esri.APP
{
    public class MapLoading : MonoBehaviour
    {

        public MapBehaviour map;
        private List<LayerBehaviour> layers;
        private int currentLayerIndex = 0;
        private float destinationAngle = 0.0f;
        private float currentAngle = 0.0f;
        private float animationDuration = 0.5f;
        private float animationStartTime = 0.0f;
        private float perspectiveAngle = 45.0f;

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
        public bool isCaseManipulating = false;
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

            if (map != null)
            {
                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    if (EventSystem.current.currentSelectedGameObject.name.Contains("Tile") && map.InputDelegate == null)
                    {
                        map.InputDelegate += UnitySlippyMap.Input.MapInput.BasicTouchAndKeyboard;
                    }
                    else if (map.InputDelegate != null)
                    {
                        map.InputDelegate -= UnitySlippyMap.Input.MapInput.BasicTouchAndKeyboard;
                    }
                }
                else
                {
                    if (map.InputDelegate == null)
                    {
                        map.InputDelegate += UnitySlippyMap.Input.MapInput.BasicTouchAndKeyboard;
                    }
                }

                GameObject.Find("Canvas/TextZoom").GetComponent<Text>().text = "Zoom Level: " + map.CurrentZoom;
                GameObject.Find("Canvas/TextTask").GetComponent<Text>().text = "Task: " + _place.Name;
                if (destinationAngle != 0.0f)
                {
                    // changing from 2D to 3D ortho angle

                    Vector3 cameraLeft = Quaternion.AngleAxis(-90.0f, Camera.main.transform.up) * Camera.main.transform.forward;
                    if ((Time.time - animationStartTime) < animationDuration)
                    {
                        float angle = Mathf.LerpAngle(0.0f, destinationAngle, (Time.time - animationStartTime) / animationDuration);
                        Camera.main.transform.RotateAround(Vector3.zero, cameraLeft, angle - currentAngle);
                        currentAngle = angle;
                    }
                    else
                    {
                        Camera.main.transform.RotateAround(Vector3.zero, cameraLeft, destinationAngle - currentAngle);
                        destinationAngle = 0.0f;
                        currentAngle = 0.0f;
                        map.IsDirty = true;
                    }

                    map.HasMoved = true;
                }
            }


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
                        //this.StartCoroutine(this.AddMap(places[i]));
                        map.CenterWGS84 = new double[2] { places[i].Location.Longitude, places[i].Location.Latitude };
                        this._place = places[i];
                        if (places[i].Name != "Default")
                        {
                            map.Zoom(80f);
                        }
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


        private IEnumerator AddMap(Place place)
        {


            map = MapBehaviour.Instance;
            map.CurrentCamera = Camera.main;

            // **MAY HAVE TO CHANGE THIS TO GET TOUCHSCRIPT WORKING** //
            map.InputDelegate += UnitySlippyMap.Input.MapInput.BasicTouchAndKeyboard;

            map.CurrentZoom = place.Level;
            // UVic

            map.CenterWGS84 = new double[2] { place.Location.Longitude, place.Location.Latitude };
            map.UsesLocation = true;
            map.InputsEnabled = true;
            map.ShowsGUIControls = true;

            //map.GUIDelegate += Toolbar;

            layers = new List<LayerBehaviour>();

            // create an Esri tile layer

            EsriTileLayer satelliteLayer = map.CreateLayer<EsriTileLayer>("Satellite");
            satelliteLayer.BaseURL = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/";
            layers.Add(satelliteLayer);

            EsriTileLayer streetLayer = map.CreateLayer<EsriTileLayer>("Street");
            streetLayer.BaseURL = "https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/";
            streetLayer.gameObject.SetActive(false);
            layers.Add(streetLayer);
            _isBuildingMap = true;

            yield return null;

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
                            places[i].Location.Longitude = double.Parse(mapinfo[1]);
                            places[i].Location.Latitude = double.Parse(mapinfo[2]);
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
                    places[i].Location.Longitude = double.Parse(task["coordinates"]["Longitude"].ToString());
                    places[i].Location.Latitude = double.Parse(task["coordinates"]["Latitude"].ToString());
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
            if (dimension.Equals("2D"))
            {
                destinationAngle = -perspectiveAngle;
            }
            else
            {
                destinationAngle = perspectiveAngle;
            }
            animationStartTime = Time.time;
        }

        public void OnChangeMapstyle(string mapstyle)
        {
            this.currentStyle = mapstyle;
            if (mapstyle.Equals("street"))
            {
                layers[0].gameObject.SetActive(false);
                layers[1].gameObject.SetActive(true);
                map.IsDirty = true;
            }
            else
            {
                layers[1].gameObject.SetActive(false);
                layers[0].gameObject.SetActive(true);
                map.IsDirty = true;
            }



        }

        public async void OnClickExit()
        {

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


            }
            Application.Quit();
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