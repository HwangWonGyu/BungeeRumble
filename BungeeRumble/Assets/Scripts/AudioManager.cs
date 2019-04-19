using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour {

    public float audioValue;

    public AudioSource startAudio;
    public AudioSource lobbyAudio;
    public AudioSource gameAudio;

	public AudioClip startBGM;
	public AudioClip lobbyBGM;
	public AudioClip gameBGM;

	public AudioClip runningClip;
	public AudioClip jumpingClip;
	public AudioClip landingClip;
	public AudioClip[] knockBackClips;
	public AudioClip[] knockBackedClips;
	public AudioClip deathClip;

	public AudioClip winClip;
	public AudioClip loseClip;

	public AudioClip menuSelectClip;
	public AudioClip readyStartClip;

	public AudioClip itemGetClip;
	public AudioClip flashBangClip;
	public AudioClip shieldClip;
	public AudioClip smokeClip;

	public static AudioManager instance = null;

	public AudioSource allSceneAudioSource;

	void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

	void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

	void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "StartScene")
        {
			lobbyAudio.clip = null;
			gameAudio.clip = null;

			startAudio.clip = startBGM;
            startAudio.volume = audioValue * 0.3f;
			startAudio.Play();
        }
        else if (scene.name == "LobbyScene")
        {
			startAudio.clip = null;
			gameAudio.clip = null;

			lobbyAudio.clip = lobbyBGM;
            lobbyAudio.volume = audioValue * 0.3f;
			lobbyAudio.Play();
        }
        else if (scene.name == "GameScene")
        {
			startAudio.clip = null;
			lobbyAudio.clip = null;

			gameAudio.clip = gameBGM;
			gameAudio.volume = audioValue * 0.3f;
            gameAudio.Play();
        }
    }


	public void AudioSet()
    {
        startAudio.volume = audioValue * 0.3f;
		lobbyAudio.volume = audioValue * 0.3f;
		gameAudio.volume = audioValue * 0.3f;

		allSceneAudioSource.volume = audioValue * 0.3f;
	}

	public void PlayReadyStartSound()
	{
		if (allSceneAudioSource.isPlaying == false)
		{
			allSceneAudioSource.clip = readyStartClip;
			allSceneAudioSource.volume = audioValue * 0.3f;
			allSceneAudioSource.Play();
		}
		else
		{
			allSceneAudioSource.clip = null;
		}
	}

	public void PlayMenuSelectSound()
	{
		if(allSceneAudioSource.isPlaying == false)
		{
			allSceneAudioSource.clip = menuSelectClip;
			allSceneAudioSource.volume = audioValue * 0.3f;
			allSceneAudioSource.Play();
		}
		else
		{
			allSceneAudioSource.clip = null;
		}
	}

	public void PlayShieldSound(AudioSource itemAudioSource)
	{
		itemAudioSource.clip = shieldClip;
		itemAudioSource.volume = audioValue;
		itemAudioSource.Play();
	}

	public void PlaySmokeSound(AudioSource itemAudioSource)
	{
		itemAudioSource.clip = smokeClip;
		itemAudioSource.volume = audioValue;
		itemAudioSource.Play();
	}

	public void PlayFlashBangSound(AudioSource itemAudioSource)
	{
		itemAudioSource.clip = flashBangClip;
		itemAudioSource.volume = audioValue;
		itemAudioSource.Play();
	}

	public void PlayRunningSound(AudioSource playerAudioSource, bool jumping, PhotonView pv)
	{
		if (jumping == false)
		{
			if (playerAudioSource.isPlaying == false)
			{
				//print("달리기 사운드 재생");
				pv.RPC("PlayRunningSoundRPC", PhotonTargets.All);
			}
		}
		else
		{
			playerAudioSource.Stop();
		}
	}

	public void StopRunningSound(AudioSource playerAudioSource)
	{
		if (playerAudioSource.isPlaying == true && playerAudioSource.clip == runningClip)
		{
			playerAudioSource.Stop();
		}
	}

	public void PlayJumpSound(AudioSource playerAudioSource)
	{
		playerAudioSource.clip = jumpingClip;
		playerAudioSource.spatialBlend = 1.0f;
		playerAudioSource.loop = false;
		playerAudioSource.volume = audioValue;
		playerAudioSource.Play();
	}

	public void PlayLandSound(AudioSource playerAudioSource)
	{
		if (playerAudioSource.clip == jumpingClip)
		{
			playerAudioSource.clip = landingClip;
			playerAudioSource.spatialBlend = 1.0f;
			playerAudioSource.loop = false;
			playerAudioSource.volume = audioValue;
			playerAudioSource.Play();
		}
	}

	public void PlayKnockBackSound(/*AudioSource playerAudioSource, string playerObjectName, */PhotonView pv)
	{
		//if (playerObjectName == "Player0(Clone)")
		//{
		//	playerAudioSource.clip = knockBackClips[0];
		//	playerAudioSource.Play();
		//}
		//else if (playerObjectName == "Player2(Clone)")
		//{
		//	playerAudioSource.clip = knockBackClips[1];
		//	playerAudioSource.Play();
		//}
		pv.RPC("PlayKnockBackSoundRPC", PhotonTargets.All);
	}

	public void PlayKnockBackedSound(/*AudioSource playerAudioSource, string playerObjectName, */PhotonView pv)
	{
		//if (playerObjectName == "Player0(Clone)")
		//{
		//	playerAudioSource.clip = knockBackedClips[0];
		//	playerAudioSource.Play();
		//}
		//else if(playerObjectName == "Player2(Clone)")
		//{
		//	playerAudioSource.clip = knockBackedClips[1];
		//	playerAudioSource.Play();
		//}
		pv.RPC("PlayKnockBackedSoundRPC", PhotonTargets.All);
	}

	public void PlayDeathSound()
	{
		Camera.main.GetComponent<AudioSource>().clip = deathClip;
		Camera.main.GetComponent<AudioSource>().volume = audioValue;
		Camera.main.GetComponent<AudioSource>().Play();
	}

	public void PlayWinSound()
	{
		gameAudio.Stop();
		Camera.main.GetComponent<AudioSource>().clip = winClip;
		Camera.main.GetComponent<AudioSource>().volume = audioValue;
		Camera.main.GetComponent<AudioSource>().Play();
	}

	public void PlayLoseSound()
	{
		gameAudio.Stop();
		Camera.main.GetComponent<AudioSource>().clip = loseClip;
		Camera.main.GetComponent<AudioSource>().volume = audioValue;
		Camera.main.GetComponent<AudioSource>().Play();
	}

	public void PlayItemGetSound(AudioSource playerAudioSource)
	{
		playerAudioSource.clip = itemGetClip;
		playerAudioSource.spatialBlend = 1.0f;
		playerAudioSource.loop = false;
		playerAudioSource.volume = audioValue;
		playerAudioSource.Play();
	}
}
