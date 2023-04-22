using System;
using UnityEngine;

namespace InfernalRobotics_v3.Effects
{
	/// <summary>
	/// credit for sound support goes to the creators of the Kerbal Attachment
	/// </summary>
	public class SoundSource
	{
		private readonly Part part;
		private readonly FXGroup motorSound;
		private bool isPlaying;

		public SoundSource(Part part, string groupId)
		{
			this.part = part;
			isPlaying = false;
			motorSound = new FXGroup(groupId);
		}

		public void Play()
		{
			if(!isPlaying && motorSound.audio)
			{
				motorSound.audio.Play();
				isPlaying = true;
			}
		}

		public bool Setup(string soundPath, bool loop, float maxDistance = 10f)
		{
			try
			{
				if(motorSound == null)
				{
					Logger.Log("motorSound FXGroup is null", Logger.Level.Warning);
					return false;
				}

				if(part == null)
				{
					Logger.Log("part is null", Logger.Level.Warning);
					return false;
				}

				if(soundPath == "")
				{
					motorSound.audio = null;
					return false;
				}
				Logger.Log(string.Format("Loading sounds : {0}", soundPath));
				if(!GameDatabase.Instance.ExistsAudioClip(soundPath))
				{
					Logger.Log("Sound file '" + soundPath + "' not found in the game database!", Logger.Level.Warning);
					motorSound.audio = null;
					return false;
				}

				motorSound.audio = part.gameObject.AddComponent<AudioSource>();
				motorSound.audio.volume = GameSettings.SHIP_VOLUME;
				motorSound.audio.rolloffMode = AudioRolloffMode.Logarithmic;
				motorSound.audio.dopplerLevel = 0f;
				//motorSound.audio.panLevel = 1f;
				motorSound.audio.maxDistance = maxDistance;
				motorSound.audio.loop = loop;
				motorSound.audio.playOnAwake = false;
				motorSound.audio.clip = GameDatabase.Instance.GetAudioClip(soundPath);
				Logger.Log("Sound successfully loaded.");
				return true;
			}
			catch(Exception ex)
			{
				Logger.Log(string.Format("SoundSource.Setup() exception {0}", ex.Message), Logger.Level.Error);
			}
			return false;
		}

		public void Stop()
		{
			if(isPlaying)
			{
				motorSound.audio.Stop();
				isPlaying = false;
			}
		}

		public void Update(float volumeSet, float pitchSet)
		{
			if(motorSound.audio == null) return;

			motorSound.audio.volume = volumeSet;
			motorSound.audio.pitch = pitchSet;
		}
	}
}