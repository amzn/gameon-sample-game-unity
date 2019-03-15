/*
 * Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify,
 * merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Collections;
using UnityEngine;

namespace Behaviors
{
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private float bottomBuffer;

        private Vector3 boundsMax = Vector3.one * Mathf.NegativeInfinity;
        private Vector3 boundsMin = Vector3.one * Mathf.Infinity;
        private Vector3 desiredPos;

        [SerializeField] private float distance = 10f;

        [SerializeField] private float horizontalBuffer;

        private float horizontalMax;
        private float horizontalMin;

        [SerializeField] private Plane movementPlane;

        private Vector3 posVelocity;

        [SerializeField] private float smooth = 10f;

        [SerializeField] private Transform target;

        [SerializeField] private float topBuffer;

        private float verticalMax;
        private float verticalMin;

        //set the target of the camera according to whats in unity's global variable in the editor
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

//start the camera pointing at the spawn point then update its position
        private void Awake()
        {
            target = GameObject.FindGameObjectWithTag("spawnpoint").transform;
            var levelTransform = FindObjectOfType<Level>().transform;
            GetBoundsRecursively(levelTransform);
            horizontalMin = boundsMin.x + horizontalBuffer;
            horizontalMax = boundsMax.x - horizontalBuffer;
            verticalMin = boundsMin.z + bottomBuffer;
            verticalMax = boundsMax.z - topBuffer;
            UpdatePosition(true);
            StartCoroutine(LateFixedUpdate());
        }

//get the boundaries of the level so the camera cant go beyond that
        private void GetBoundsRecursively(Transform parent)
        {
            foreach (Transform child in parent)
            {
                var renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    boundsMin = Vector3.Min(boundsMin, renderer.bounds.min);
                    boundsMax = Vector3.Max(boundsMax, renderer.bounds.max);
                }

                if (child.childCount > 0)
                    GetBoundsRecursively(child);
            }
        }

//update the position of the camera depending on the position of the target
        private void UpdatePosition(bool immediate)
        {
            var pos = target.position + Vector3.up * distance;
            pos = new Vector3
            {
                x = horizontalMin > horizontalMax
                    ? (horizontalMin + horizontalMax) * 0.5f
                    : Mathf.Clamp(pos.x, horizontalMin, horizontalMax),
                y = pos.y,
                z = verticalMin > verticalMax
                    ? (verticalMin + verticalMax) * 0.5f
                    : Mathf.Clamp(pos.z, verticalMin, verticalMax)
            };
            transform.position = immediate ? pos : Vector3.SmoothDamp(transform.position, pos, ref posVelocity, smooth);
        }

//late fixed update that determines when to fix the camera position and update it
        private IEnumerator LateFixedUpdate()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();
                if (target != null) UpdatePosition(false);
            }
        }
    }
}