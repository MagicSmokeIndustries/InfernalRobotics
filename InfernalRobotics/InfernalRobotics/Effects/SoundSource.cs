using UnityEngine;

namespace InfernalRobotics.Effects
{
    public class SoundSource
    {
        public FXGroup MotorSound { get; set; }
        public bool IsPlaying { get; set; }

        public SoundSource()
        {
            IsPlaying = false;
        }

        public void PlayAudio()
        {
            if (!IsPlaying && MotorSound.audio)
            {
                MotorSound.audio.Play();
                IsPlaying = true;
            }
        }

        public bool CreateFxSound(Part part, string sndPath, bool loop, float maxDistance = 10f)
        {
            if (sndPath == "")
            {
                MotorSound.audio = null;
                return false;
            }
            Debug.Log("Loading sounds : " + sndPath);
            if (!GameDatabase.Instance.ExistsAudioClip(sndPath))
            {
                Debug.Log("Sound not found in the game database!");
                //ScreenMessages.PostScreenMessage("Sound file : " + sndPath + " as not been found, please check your Infernal Robotics installation!", 10, ScreenMessageStyle.UPPER_CENTER);
                MotorSound.audio = null;
                return false;
            }
            MotorSound.audio = part.gameObject.AddComponent<AudioSource>();
            MotorSound.audio.volume = GameSettings.SHIP_VOLUME;
            MotorSound.audio.rolloffMode = AudioRolloffMode.Logarithmic;
            MotorSound.audio.dopplerLevel = 0f;
            MotorSound.audio.panLevel = 1f;
            MotorSound.audio.maxDistance = maxDistance;
            MotorSound.audio.loop = loop;
            MotorSound.audio.playOnAwake = false;
            MotorSound.audio.clip = GameDatabase.Instance.GetAudioClip(sndPath);
            Debug.Log("Sound successfully loaded.");
            return true;
        }

        public void StopAudio()
        {
            MotorSound.audio.Stop();
            IsPlaying = false;
        }

        public void UpdateSound(float soundSet, float pitchSet)
        {
            MotorSound.audio.volume = soundSet;
            MotorSound.audio.pitch = pitchSet;
        }
    }
}
