using System;
using System.IO;

using UnityEngine;

using UnitySlippyMap.Helpers;

namespace UnitySlippyMap.Layers
{
    public class EsriTileLayer : WebTileLayerBehaviour
    {
        #region Private members & properties


        /// <summary>
        /// The format for the URL parameters as in String.Format().
        /// </summary>
        /// https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{z}/{y}/{x}
        /// World_Street_Map
        /// 

        private string urlParametersFormat = "{0}/{2}/{1}";

        /// <summary>
        /// Gets or sets the URL parameters format.
        /// </summary>
        /// <value>The format for the URL parameters as in String.Format().</value>
        public string URLParametersFormat
        {
            //returns the private global string
            get { return urlParametersFormat; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value == String.Empty)
                    throw new ArgumentException("value cannot be empty");
                urlParametersFormat = value;
            }
        }

        /// <summary>
        /// The extension of the tile files.
        /// </summary>
        private string tileImageExtension = ".png";

        /// <summary>
        /// Gets or sets the tile image extension.
        /// </summary>
        /// <value>The extension of the tile files.</value>
        public string TileImageExtension
        {
            get { return tileImageExtension; }
            set
            {
                tileImageExtension = value;
                if (tileImageExtension == null)
                    tileImageExtension = String.Empty;
            }
        }

        #endregion

        #region EsriTileLayer implementation

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitySlippyMap.Layers.EsriTileLayer"/> class.
        /// </summary>
        public EsriTileLayer()
        {
            isReadyToBeQueried = true;
        }

        #endregion

        #region MonoBehaviour implementation

        /// <summary>
        /// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Awake().
        /// </summary>
        private new void Awake()
        {
            base.Awake();
            minZoom = 1;
            maxZoom = 19;
        }

        #endregion

        #region TileLayer implementation

        /// <summary>
        /// Gets the tile count per axis. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.GetTileCountPerAxis"/>.
        /// </summary>
        /// <param name="tileCountOnX">Tile count on x.</param>
        /// <param name="tileCountOnY">Tile count on y.</param>
        protected override void GetTileCountPerAxis(out int tileCountOnX, out int tileCountOnY)
        {
            tileCountOnX = tileCountOnY = (int)Mathf.Pow(2, Map.RoundedZoom);
        }

        /// <summary>
        /// Gets the center tile. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.GetCenterTile"/>.
        /// </summary>
        /// <param name="tileCountOnX">Tile count on x.</param>
        /// <param name="tileCountOnY">Tile count on y.</param>
        /// <param name="tileX">Tile x.</param>
        /// <param name="tileY">Tile y.</param>
        /// <param name="offsetX">Offset x.</param>
        /// <param name="offsetZ">Offset z.</param>
        protected override void GetCenterTile(int tileCountOnX, int tileCountOnY, out int tileX, out int tileY, out float offsetX, out float offsetZ)
        {
            int[] tileCoordinates = GeoHelpers.WGS84ToTile(Map.CenterWGS84[0], Map.CenterWGS84[1], Map.RoundedZoom);
            double[] centerTile = GeoHelpers.TileToWGS84(tileCoordinates[0], tileCoordinates[1], Map.RoundedZoom);
            double[] centerTileMeters = Map.WGS84ToEPSG900913Transform.Transform(centerTile); //GeoHelpers.WGS84ToMeters(centerTile[0], centerTile[1]);

            tileX = tileCoordinates[0];
            tileY = tileCoordinates[1];
            offsetX = Map.RoundedHalfMapScale / 2.0f - (float)(Map.CenterEPSG900913[0] - centerTileMeters[0]) * Map.RoundedScaleMultiplier;
            offsetZ = -Map.RoundedHalfMapScale / 2.0f - (float)(Map.CenterEPSG900913[1] - centerTileMeters[1]) * Map.RoundedScaleMultiplier;
        }

        /// <summary>
        /// Gets a neighbour tile. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.GetNeighbourTile"/>.
        /// </summary>
        /// <returns><c>true</c>, if neighbour tile was gotten, <c>false</c> otherwise.</returns>
        /// <param name="tileX">Tile x.</param>
        /// <param name="tileY">Tile y.</param>
        /// <param name="offsetX">Offset x.</param>
        /// <param name="offsetZ">Offset z.</param>
        /// <param name="tileCountOnX">Tile count on x.</param>
        /// <param name="tileCountOnY">Tile count on y.</param>
        /// <param name="dir">Dir.</param>
        /// <param name="nTileX">N tile x.</param>
        /// <param name="nTileY">N tile y.</param>
        /// <param name="nOffsetX">N offset x.</param>
        /// <param name="nOffsetZ">N offset z.</param>
        protected override bool GetNeighbourTile(int tileX, int tileY, float offsetX, float offsetZ, int tileCountOnX, int tileCountOnY, NeighbourTileDirection dir, out int nTileX, out int nTileY, out float nOffsetX, out float nOffsetZ)
        {
            bool ret = false;
            nTileX = 0;
            nTileY = 0;
            nOffsetX = 0.0f;
            nOffsetZ = 0.0f;

            switch (dir)
            {
                case NeighbourTileDirection.South:
                    if ((tileY + 1) < tileCountOnY)
                    {
                        nTileX = tileX;
                        nTileY = tileY + 1;
                        nOffsetX = offsetX;
                        nOffsetZ = offsetZ - Map.RoundedHalfMapScale;
                        ret = true;
                    }
                    break;

                case NeighbourTileDirection.North:
                    if (tileY > 0)
                    {
                        nTileX = tileX;
                        nTileY = tileY - 1;
                        nOffsetX = offsetX;
                        nOffsetZ = offsetZ + Map.RoundedHalfMapScale;
                        ret = true;
                    }
                    break;

                case NeighbourTileDirection.East:
                    nTileX = tileX + 1;
                    nTileY = tileY;
                    nOffsetX = offsetX + Map.RoundedHalfMapScale;
                    nOffsetZ = offsetZ;
                    ret = true;
                    break;

                case NeighbourTileDirection.West:
                    nTileX = tileX - 1;
                    nTileY = tileY;
                    nOffsetX = offsetX - Map.RoundedHalfMapScale;
                    nOffsetZ = offsetZ;
                    ret = true;
                    break;
            }


            return ret;
        }

        #endregion

        #region WebTileLayer implementation

        /// <summary>
        /// Gets a tile URL. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.GetTileURL"/>.
        /// </summary>
        /// <returns>The tile URL.</returns>
        /// <param name="tileX">Tile x.</param>
        /// <param name="tileY">Tile y.</param>
        /// <param name="roundedZoom">Rounded zoom.</param>
        protected override string GetTileURL(int tileX, int tileY, int roundedZoom)
        {
            return String.Format(Path.Combine(BaseURL, URLParametersFormat).Replace("\\", "/") + TileImageExtension, roundedZoom, tileX, tileY);
        }

        #endregion
    }
}


