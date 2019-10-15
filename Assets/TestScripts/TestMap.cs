// 
//  TestMap.cs
//  
//  Author:
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
//  Copyright (c) 2012 Jonathan Derrough
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

// #define DEBUG_LOG

using UnityEngine;

using System;

using UnitySlippyMap.Map;
using UnitySlippyMap.Markers;
using UnitySlippyMap.Layers;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Converters.WellKnownText;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.UI;

public class TestMap : MonoBehaviour
{

    //public Button myZoomButton;

    private MapBehaviour		map;
	
	public Texture	LocationTexture;
	public Texture	MarkerTexture;

	
	private float	guiXScale;
	private float	guiYScale;
	private Rect	guiRect;
	
	private bool 	isPerspectiveView = false;
	private float	perspectiveAngle = 30.0f;
	private float	destinationAngle = 0.0f;
	private float	currentAngle = 0.0f;
	private float	animationDuration = 0.5f;
	private float	animationStartTime = 0.0f;



    private List<LayerBehaviour> layers;
    private int     currentLayerIndex = 0;

    private string utfGridJsonString = "";

    
	bool Toolbar(MapBehaviour map)
	{
		GUI.matrix = Matrix4x4.Scale(new Vector3(guiXScale, guiXScale, 1.0f));
		
		GUILayout.BeginArea(guiRect);
		
		GUILayout.BeginHorizontal();
		
		GUILayout.Label("Zoom: " + map.CurrentZoom);
		
		bool pressed = false;
        /*
        myZoomButton.onClick.AddListener(ZoomIn);
        void ZoomIn()
        {
            Debug.Log("You clicked Zoom.");
        }
        */

        if (GUILayout.RepeatButton("+", GUILayout.ExpandHeight(true)))
		{
			map.Zoom(1.0f);
			pressed = true;
		}
        if (Event.current.type == EventType.Repaint)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            if (rect.Contains(Event.current.mousePosition))
                pressed = true;
        }

        if (GUILayout.Button("2D/3D", GUILayout.ExpandHeight(true)))
		{
			if (isPerspectiveView)
			{
				destinationAngle = -perspectiveAngle;
			}
			else
			{
				destinationAngle = perspectiveAngle;
			}
			
			animationStartTime = Time.time;
			
			isPerspectiveView = !isPerspectiveView;
		}
      
        if (Event.current.type == EventType.Repaint)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            if (rect.Contains(Event.current.mousePosition))
                pressed = true;
        }

        string layerMessage = String.Empty;
        if (map.CurrentZoom > layers[currentLayerIndex].MaxZoom)
            layerMessage = "\nZoom out!";
        else if (map.CurrentZoom < layers[currentLayerIndex].MinZoom)
            layerMessage = "\nZoom in!";
        if (GUILayout.Button(((layers != null && currentLayerIndex < layers.Count) ? layers[currentLayerIndex].name + layerMessage : "Layer"), GUILayout.ExpandHeight(true)))
        {

            layers[currentLayerIndex].gameObject.SetActive(false);
            ++currentLayerIndex;
            if (currentLayerIndex >= layers.Count)
            {
                currentLayerIndex = 0;
            }
            layers[currentLayerIndex].gameObject.SetActive(true);

            map.IsDirty = true;
        }

        if (GUILayout.RepeatButton("-", GUILayout.ExpandHeight(true)))
		{
			map.Zoom(-1.0f);
			pressed = true;
		}

        if (Event.current.type == EventType.Repaint)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            if (rect.Contains(Event.current.mousePosition))
                pressed = true;
        }

        GUILayout.EndHorizontal();
					
		GUILayout.EndArea();

        // Show any mbtiles utf string under the mouse position
        if (!string.IsNullOrEmpty(utfGridJsonString))
            GUILayout.Label(utfGridJsonString);

		return pressed;

	}
    
	
	private IEnumerator Start()
	{

        //myZoomButton = GameObject.Find("Button_zoom_+").GetComponent<Button>();

        // setup the gui scale according to the screen resolution
        guiXScale = (Screen.orientation == ScreenOrientation.Landscape ? Screen.width : Screen.height) / 960;
        guiYScale = (Screen.orientation == ScreenOrientation.Landscape ? Screen.height : Screen.width) / 1280;
		// setup the gui area
		guiRect = new Rect(16.0f * guiXScale, 4.0f * guiXScale, Screen.width / guiXScale - 32.0f * guiXScale, 32.0f * guiYScale);

		// create the map singleton
		map = MapBehaviour.Instance;
		map.CurrentCamera = Camera.main;

        // **MAY HAVE TO CHANGE THIS TO GET TOUCHSCRIPT WORKING** //
        map.InputDelegate += UnitySlippyMap.Input.MapInput.BasicTouchAndKeyboard;

		map.CurrentZoom = 10.0f;
		// UVic

		map.CenterWGS84 = new double[2] { -123.310900, 48.460959 };
		map.UsesLocation = true;
		map.InputsEnabled = true;
		map.ShowsGUIControls = true;

		map.GUIDelegate += Toolbar;

        layers = new List<LayerBehaviour>();

		// create an Esri tile layer

      

        EsriTileLayer satelliteLayer = map.CreateLayer<EsriTileLayer>("Satellite");
        satelliteLayer.BaseURL = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/";
        layers.Add(satelliteLayer);

        EsriTileLayer streetLayer = map.CreateLayer<EsriTileLayer>("Street");
        streetLayer.BaseURL = "https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/";
        streetLayer.gameObject.SetActive(false);
        layers.Add(streetLayer);

        
        

        yield return null;
	
	}
	
	void OnApplicationQuit()
	{
		map = null;
	}
	
	void Update()
	{
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

    

#if DEBUG_PROFILE
	void LateUpdate()
	{
		Debug.Log("PROFILE:\n" + UnitySlippyMap.Profiler.Dump());
		UnitySlippyMap.Profiler.Reset();
	}
#endif
}

