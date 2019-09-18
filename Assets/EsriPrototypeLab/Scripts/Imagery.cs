﻿/*
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
using UnityEngine;
using UnityEngine.Networking;

namespace Esri.PrototypeLab.HoloLens.Unity {
    public static class Imagery {     
        public static IEnumerator GetTexture(Tile tile, string dimension, Action<Texture2D> callback) {
            // Construct url to AGOL imagery tile.
            string URL;
            if (dimension == "satellite")
            {
                URL = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer";
            }
            else
            {
                URL = "http://elevation3d.arcgis.com/arcgis/rest/services/World_Street_Map/MapServer";
            }

            string url = string.Format("{0}/tile/{1}/{2}/{3}", new object[] {
                URL,
                tile.Zoom,
                tile.Y,
                tile.X
            });

            // Download image tile.
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            //WWW www = new WWW(url);
            //yield return www;
            yield return www.SendWebRequest();

            // Load image as texture.
            Texture2D texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
            texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            //www.LoadImageIntoTexture(texture);
            yield return null;

            // return.
            callback(texture);
        }
    }
}
