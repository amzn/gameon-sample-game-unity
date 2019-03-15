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

using System.Collections.Generic;
using UnityEngine;

namespace Behaviors
{
    public class RacemanBehavior : MonoBehaviour
    {
        private Animation anim;
        private new AudioSource audio;
        private CapsuleCollider capsuleCollider;
        private float colliderOriginalHeight;
        private FollowCamera followCam;
        private int framesSinceThrust;
        public float maxMoveSpeed, thrustVelocity, slowdownDrag, swipeThreshold;
        private Rigidbody rb;
        public GameObject smoke, smokeSpawn, flash, explosion;
        private bool swipeComplete;
        private Vector2 swipeEnd;
        private Vector2 swipeStart;
        private bool swiping;
        private Vector2 thrustDirection;
        public int thrustFrames, slowdownFrames, smokerate;
        private bool thrusting, isGrounded, isAlive;
        public AudioClip thrustingSound;

        //set some initial values
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            isAlive = true;
            thrusting = false;
            isGrounded = false;
            framesSinceThrust = 0;
            audio = GetComponent<AudioSource>();
            anim = GetComponentInChildren<Animation>();
            followCam = FindObjectOfType<FollowCamera>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            colliderOriginalHeight = capsuleCollider.height;
            rb = GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.useGravity = true;
            rb.drag = 0;
            anim.Play("rig|JumpFall");
        }

// this function checks for swipes or directional buttons on a keyboard
        private bool CheckForSwipes(ref Vector2 direction)
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.UpArrow))
                direction = Vector2.up;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                direction = Vector2.down;
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
                direction = Vector2.left;
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                direction = Vector2.right;
            if (direction != Vector2.zero)
                return true;
#endif
            var dragDistance = Screen.height * (swipeThreshold * 0.01f);
            if (Input.GetMouseButton(0))
            {
                if (!swipeComplete)
                {
                    if (!swiping)
                    {
                        swipeStart = swipeEnd = Input.mousePosition;
                        swiping = true;
                    }
                    else
                    {
                        swipeEnd = Input.mousePosition;
                    }
                }
            }
            else
            {
                swiping = swipeComplete = false;
            }

            var swipeVector = swipeEnd - swipeStart;
            if (!swiping || !(swipeVector.magnitude > dragDistance)) return false;
            direction = GetClosestCardinalVector(swipeVector);
            swipeComplete = true;
            swiping = false;
            return true;
        }

        private Vector2 GetClosestCardinalVector(Vector2 input)
        {
            var dir = new List<KeyValuePair<float, Vector2>>
            {
                new KeyValuePair<float, Vector2>(Vector2.Dot(Vector2.up, input), Vector2.up),
                new KeyValuePair<float, Vector2>(Vector2.Dot(Vector2.down, input), Vector2.down),
                new KeyValuePair<float, Vector2>(Vector2.Dot(Vector2.left, input), Vector2.left),
                new KeyValuePair<float, Vector2>(Vector2.Dot(Vector2.right, input), Vector2.right)
            };
            dir.Sort((d1, d2) => d1.Key.CompareTo(d2.Key));
            return dir[dir.Count - 1].Value;
        }

//check for swipes at each frame and move the player
        private void Update()
        {
            var swipeDirection = Vector2.zero;
            if (CheckForSwipes(ref swipeDirection)) Thrust(swipeDirection);
        }

// set the velocity of the player at every physics step
        private void FixedUpdate()
        {
            if (thrusting)
            {
                framesSinceThrust++;
                if (framesSinceThrust % smokerate == 0)
                {
                    //Instantiate(smoke, new Vector3(smokeSpawn.transform.position.x, smokeSpawn.transform.position.y, smokeSpawn.transform.position.z), smokeSpawn.transform.rotation);
                }

                if (framesSinceThrust <= thrustFrames)
                {
                    rb.velocity = thrustVelocity * new Vector3(thrustDirection.x, 0, thrustDirection.y);
                }
                else
                {
                    anim.CrossFade("rig|JumpFall", 0.5f);
                    rb.drag = slowdownDrag;
                    if (framesSinceThrust > thrustFrames + slowdownFrames)
                        thrusting = false;
                }
            }

            if (thrusting) return;
            capsuleCollider.height = colliderOriginalHeight;
            framesSinceThrust = 0;
            rb.useGravity = true;
            rb.drag = 0;
            if (isGrounded) rb.velocity = Vector3.Scale(rb.velocity, new Vector3(0, 1, 0));
        }

//actually move the character
        private void Thrust(Vector2 direction)
        {
            capsuleCollider.height = colliderOriginalHeight - 0.1f;
            framesSinceThrust = 0;
            rb.useGravity = false;
            rb.drag = 0;
            thrusting = true;
            isGrounded = false;
            anim.CrossFade("rig|Jump", 0.05f);
            if (direction == Vector2.left)
                transform.localEulerAngles = new Vector3(0, 0, 0);
            else if (direction == Vector2.right) transform.localEulerAngles = new Vector3(0, 0, 180);
            thrustDirection = direction;
            audio.clip = thrustingSound;
            audio.Play();
        }

//what happens when a player collides with other objects. die when its a hazard.
        private void OnCollisionEnter(Collision collision)
        {
            if (!isAlive || !collision.gameObject.CompareTag("hazard")) return;
            isAlive = false;
            KillMe();
        }

//kill the player
        private void KillMe()
        {
            followCam.SetTarget(Instantiate(explosion, transform.position, transform.rotation).transform);
            rb.velocity = Vector3.zero;
            Destroy(gameObject);
            FindObjectOfType<LevelManager_rescuethem>().Respawn();
        }

//what happens when a player is no longer on the ground
        private void OnCollisionExit(Collision collision)
        {
            foreach (var contact in collision.contacts) // no longer grounded
                if (Vector3.Dot(contact.normal, Vector3.forward) > 0.9f)
                    isGrounded = false;
        }

//what happens when the player is on the ground
        private void OnCollisionStay(Collision collision)
        {
            foreach (var contact in collision.contacts)
            {
                if (!(Vector3.Dot(contact.normal, Vector3.forward) > 0.9f)) continue;
                if (thrusting) continue;
                isGrounded = true;
                anim.CrossFade("rig|Idle", 0.2f);
                rb.drag = 0;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            var bounds = GetComponent<Collider>().bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.extents * 2f);
        }
    }
}