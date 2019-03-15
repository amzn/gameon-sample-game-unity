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

using UnityEngine;

namespace Behaviors
{
    public class NewSpawnPoint : MonoBehaviour
    {
        public Material background;
        public Material checkpointOff;
        public Material checkpointOn;

        private LevelManager_rescuethem manager;

        // Used this for initialization
        private void Start()
        {
            manager = GameObject.FindGameObjectWithTag("level manager").GetComponent<LevelManager_rescuethem>();
        }

        // this function makes is so that a new check point is the new spawn point after a player passes through it.
        private void OnTriggerEnter(Collider trigger)
        {
            if (!trigger.gameObject.CompareTag("Player")) return;
            manager.spawnPoint = gameObject;
            foreach (var checkpoint in FindObjectsOfType<NewSpawnPoint>())
            {
                var renderer = checkpoint.GetComponentInChildren<Renderer>();
                if (renderer == null) continue;
                var m = renderer.materials;
                m[0] = background;
                m[1] = checkpoint == this ? checkpointOn : checkpointOff;
                renderer.materials = m;
            }
        }
    }
}