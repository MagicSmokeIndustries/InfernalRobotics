using System;
using UnityEngine;

namespace InfernalRobotics.Effects
{
    /// <summary>
    /// credit for sound support goes to the creators of the Kerbal Attachment 
    /// </summary>
    public class SoundSource
    {
        private readonly Part part;
        private FXGroup motorSound;
        private bool isPlaying;

        public SoundSource(Part part, string groupId)
        {
            this.part = part;
            isPlaying = false;
            motorSound = new FXGroup(groupId);
        }

        public void Play()
        {
            if (!isPlaying && motorSound.audio)
            {
                motorSound.audio.Play();
                isPlaying = true;
            }
        }

        public bool Setup(string sndPath, bool loop, float maxDistance = 10f)
        {
            try
            {
                if (this.motorSound == null)
                {
                    Debug.Log("motorSound FXGroup is null");
                    return false;
                }

                if (this.part == null)
                {
                    Debug.Log("part is null");
                    return false;
                }

                if (sndPath == "")
                {
                    motorSound.audio = null;
                    return false;
                }
                Debug.Log("Loading sounds : " + sndPath);
                if (!GameDatabase.Instance.ExistsAudioClip(sndPath))
                {
                    Debug.Log("Sound not found in the game database!");
                    //ScreenMessages.PostScreenMessage("Sound file : " + sndPath + " as not been found, please check your Infernal Robotics installation!", 10, ScreenMessageStyle.UPPER_CENTER);
                    motorSound.audio = null;
                    return false;
                }
                
                motorSound.audio = part.gameObject.AddComponent<AudioSource>();
                motorSound.audio.volume = GameSettings.SHIP_VOLUME;
                motorSound.audio.rolloffMode = AudioRolloffMode.Logarithmic;
                motorSound.audio.dopplerLevel = 0f;
                motorSound.audio.panLevel = 1f;
                motorSound.audio.maxDistance = maxDistance;
                motorSound.audio.loop = loop;
                motorSound.audio.playOnAwake = false;
                motorSound.audio.clip = GameDatabase.Instance.GetAudioClip(sndPath);
                Debug.Log("Sound successfully loaded.");
                return true;
            }
            catch (Exception ex)
            {
               
                Debug.LogError(string.Format("SoundSource.Setup() exception {0}", ex.Message));
                
            }
            return false;
        }

        public void Stop()
        {
            if (isPlaying)
            {
                motorSound.audio.Stop();
                isPlaying = false;
            }
        }

        public void Update(float soundSet, float pitchSet)
        {
            if (motorSound.audio == null) return;

            motorSound.audio.volume = soundSet;
            motorSound.audio.pitch = pitchSet;
        }
    }
}
