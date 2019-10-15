using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Esri.APP
{
    public class ViewChanging : MonoBehaviour
    {

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool _needUpdate;
        public float movingSpeed = 50f;


        // Start is called before the first frame update
        private void Start()
        {
            targetPosition = this.transform.localPosition;
            _needUpdate = false;
        }

        // Update is called once per frame
        void Update()
        {
            float step = movingSpeed * Time.deltaTime;
            if (this.transform.localPosition != targetPosition && _needUpdate)
            {
                this.transform.localPosition = Vector3.MoveTowards(this.transform.localPosition, targetPosition, step);
            }
            if (this.transform.rotation != targetRotation && _needUpdate)
            {
                this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRotation, step);
            }

            if (this.transform.localPosition == targetPosition && this.transform.rotation == targetRotation)
            {
                _needUpdate = false;
            }
        }

        public void OnChangeDimension(string dimension)
        {
            //once receive event, update the position and rotation. 
            _needUpdate = true;

            if (dimension == "3D")
            {
                targetPosition = new Vector3(this.transform.position.x, this.transform.position.y - 30f, this.transform.position.z + 25f);
                targetRotation = Quaternion.Euler(30.0f, 180.0f, 0f);
            }
            else
            {
                targetPosition = new Vector3(0f, 40f, 0f);
                targetRotation = Quaternion.Euler(90.0f, 180.0f, 0f);
            }

        }

    }
}
