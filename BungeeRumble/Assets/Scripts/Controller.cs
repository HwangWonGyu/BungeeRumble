using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]

public class Controller : MonoBehaviour
{
	[SerializeField] private bool m_IsWalking;
	[SerializeField] private float m_WalkSpeed;
	[SerializeField] private float m_RunSpeed;
	[SerializeField] private float m_JumpSpeed;
	[SerializeField] private float m_StickToGroundForce; // *중요 : CharacterController.isGrounded를 true로 만들어주기위한 Vector3.y 의 음수값
	[SerializeField] private float m_GravityMultiplier;
	[SerializeField] private float m_RayDistance;
	[SerializeField] private GameObject m_RayStart;
	[SerializeField] private float knockBackPower;
	private float initialKnockBackPower;
	[SerializeField] private float m_MaxFieldOfView;
	[SerializeField] private float m_MinFieldOfView;

	// Photon
	private PhotonView pv = null;
	private PhotonTransformView ptv = null;

	// Character Moving
	private CharacterController characterController;
	private Vector2 input;
	private Vector3 moveDir = Vector3.zero;
	private Vector3 synchronizeSpeed;
	private float initialGravityMultiplier;

	// Camera
	public Camera cam; // DeadPlayerCamera에서 참조하니까 프로퍼티 써야하나?
	private float cameraFieldOfViewControll; // 카메라 Field Of View 증가 감소 판별하는 변수
	private float offsetCameraCharacter;
	private MouseLook mouseLook;
	private RaycastHit movingMapHitInfo;
	private RaycastHit unmovingMapHitInfo;
	private GameObject blindMap;

	// Jump
	private bool jump;
	private bool chargeJump;
	private bool doubleJump;
	private bool jumping;
	private bool previouslyGrounded;
	private int jumpCount; // 2단 점프 가능여부 판별하는 변수

	// Knockback
	private RaycastHit hit;
	private bool isKnockbacked;
	private Vector3 knockBackDir;

	// Animation
	private Animator animator;

	// Item
	private ItemThrow itemLine;
	[HideInInspector] public bool jumpItem; // ItemManager에서 참조하니까 프로퍼티 써야하나?
	[HideInInspector] public bool isShield; // ItemManager에서 참조하니까 프로퍼티 써야하나?
	[SerializeField] private GameObject particleWave;
	[SerializeField] private GameObject particleJump;
	public GameObject usedItem;
	private bool onceCountDown;

	// Sound
	private AudioSource runAudioSource;
	private AudioSource jumpAudioSource;
	private AudioSource knockBackAudioSource;
	private AudioSource knockBackedAudioSource;
	private AudioListener audioListener;
	private bool onceKnockBackSound;

	// Countdown
	private float gameStartTime;
	[SerializeField] private float isMovableTime;

	private void Awake()
	{
		pv = GetComponent<PhotonView>();
		ptv = GetComponent<PhotonTransformView>();
		animator = GetComponentInChildren<Animator>();
		AudioSource[] audioSources = GetComponents<AudioSource>();
		runAudioSource = audioSources[0];
		jumpAudioSource = audioSources[1];
		knockBackAudioSource = audioSources[2];
		knockBackedAudioSource = audioSources[3];

		audioListener = GetComponentInChildren<AudioListener>();

		if (pv.isMine)
		{
			jumping = false;
			doubleJump = false;
			cam = GetComponentInChildren<Camera>();
			characterController = GetComponent<CharacterController>();
			mouseLook = GetComponent<MouseLook>();
			itemLine = GetComponent<ItemThrow>();

			mouseLook.Init(transform, cam.transform);
		}
		else
		{
			cam = GetComponentInChildren<Camera>();
			cam.gameObject.SetActive(false);
			// 나를 제외한 나머지 캐릭터들의 AudioListener를 꺼줌
			audioListener.enabled = false;
		}

		// 게임 시작 시간은 플레이어 프리팹의 인스턴스화 직후 PhotonNetwork.time으로 설정
		gameStartTime = (float)PhotonNetwork.time;

		initialGravityMultiplier = m_GravityMultiplier;

		// 초기의 knockBackPower를 저장해둠
		initialKnockBackPower = knockBackPower;
	}

	// use this for initialization
	private void Update()
	{
		if (!pv.isMine)
			return;

		RotateView();

		ResizeFieldOfView();

		CheckCharacterMoveState();
	}

	private void FixedUpdate()
	{
		if (!pv.isMine)
			return;

		// 게임 시작 후 isActiveMoveTime 뒤에 움직일 수 있음
		if ((float)PhotonNetwork.time - gameStartTime >= isMovableTime)
		{
			MoveCharacter();

			for (int i = 0; i < UIManager.instance.initialAllPlayerActorIDList.Count; i++)
			{
				if (PhotonNetwork.player.ID == UIManager.instance.initialAllPlayerActorIDList[i])
				{
					// 다른 플레이어들에게 내 컬러 보이게 해주기
					pv.RPC("ColorUIRPC", PhotonTargets.Others, i);
				}
			}
		}
		// 게임 시작 후 isActiveMoveTime 이전이라면 그 시간에 맞게 카운트다운 텍스트 띄워줌
		else
		{
			UIManager.instance.gameStartCountDownText.GetComponent<Text>().text = (isMovableTime - ((float)PhotonNetwork.time - gameStartTime)).ToString("0");
			// 0초가 됐다면 일정 시간 뒤에 카운트다운 텍스트 사라지게 함
			if ((isMovableTime - ((float)PhotonNetwork.time - gameStartTime)).ToString("0") == "0" && onceCountDown == false)
			{
				StartCoroutine(RemoveStartCountText(0.7f));
			}
		}

		// 마우스 좌클릭 하면 && 원거리 생성 아이템 사용중이 아니라면
		if (Input.GetMouseButtonDown(0) && itemLine.enabled == false)
		{
			AudioManager.instance.PlayKnockBackSound(/*knockBackAudioSource, this.gameObject.name, */pv);
			KnockBackAttack();
		}

		// 넉백당할 권한이 있으면 && 쉴드 아이템 사용중이 아니라면
		if (isKnockbacked == true && isShield == false)
		{
			StartCoroutine(OnceKnockBackedSound());
			KnockBackAttacked();
			print("맞았다");
		}

		// 넉백당했고 && 쉴드 아이템을 사용중이라면
		if (isKnockbacked == true && isShield == true)
		{
			print("막았다");
			isKnockbacked = false;
			pv.RPC("ParticleDestroyRPC", PhotonTargets.All, pv.viewID);
			pv.RPC("ShiledRPC", PhotonTargets.All, 0, pv.viewID);
		}

		//RotateView()의 LookRotation() 내부에 이미 호출코드 있어서 주석처리함
		//mouseLook.UpdateCursorLock();

		CheckCameraBlindness();

		CheckMovingMap();
	}

	IEnumerator OnceKnockBackedSound()
	{
		while (onceKnockBackSound == false)
		{
			print("한번 재생?");
			AudioManager.instance.PlayKnockBackedSound(/*knockBackedAudioSource, this.gameObject.name, */pv);
			onceKnockBackSound = true;
		}
		yield return new WaitForSeconds(1.0f);
		onceKnockBackSound = false;
	}

	[PunRPC]
	private void ColorUIRPC(int playerNum)
	{
		// 자식에 있는 컬러 Image들을 모두 받아옴
		Image[] colorImages = GetComponentsInChildren<Image>();
		// actorID 순서에 맞는 컬러 Image를 활성화
		colorImages[playerNum].enabled = true;
	}

	IEnumerator RemoveStartCountText(float waitTime)
	{
		onceCountDown = true;
		yield return new WaitForSeconds(waitTime);
		UIManager.instance.gameStartCountDownText.GetComponent<Text>().text = null;
	}

	private void LateUpdate()
	{
		//캐릭터 종류에 따라 코드 어떻게 해야할지 생각해보기
		//다른 애니메이션 실행 시점 생각해보기
		//예) 점프 동작 끝나고 나서 여전히 공중일때 -> 점프 동작의 시간 만큼 지나면 떨어지는 애니메이션 실행?

		if (!pv.isMine)
			return;

		// WASD로 조금이라도 움직이면
		if (Input.GetAxis("Horizontal") != 0.0f || Input.GetAxis("Vertical") != 0.0f)
		{
			animator.SetBool("Run", true);

			//print("달리기 사운드 재생");
			// 점프 중이 아니라면 달리기 사운드 재생
			AudioManager.instance.PlayRunningSound(runAudioSource, jumping, pv);
		}
		// 가만히 있으면
		else
		{
			animator.SetBool("Run", false);
			// 달리기 사운드 종료
			AudioManager.instance.StopRunningSound(runAudioSource);
		}

		// 점프 중이면
		if (jumping == true && animator.GetBool("Jump") == false)
		{
			animator.SetBool("Jump", true);
			StartCoroutine(SetBoolAfter("Fall", true, 1.0f)); // 상수 넘겨주는거 안좋을듯, 해당 애니메이션의 진행도 얻어와서 생각해보기
		}

		// 점프 중이 아니라면
		else if (jumping == false && animator.GetBool("Jump") == true)
		{
			animator.SetBool("Jump", false);
		}

		// 왼쪽 마우스 버튼 눌렀고 아이템 가이드라인 미사용중이라면
		if (Input.GetMouseButtonDown(0) && itemLine.enabled == false)
		{
			animator.SetBool("Attack", true);
			// 이게 맞나?
			StartCoroutine(SetBoolAfter("Attack", false, 1.0f)); // 상수 넘겨주는거 안좋을듯, 해당 애니메이션의 진행도 얻어와서 생각해보기
		}


		// 점프키 안눌렀을때 && 점프 상태 아닐때 && 땅에 닿아있을때
		if (jump == false && jumping == false && characterController.isGrounded == true)
		{
			//print("언제 호출됨?");
			animator.SetBool("Fall", false);
		}
		// 땅에 안닿아있을때 && 점프 상태 아닐때 = 그냥 떨어지고 있을때
		// 참고 : 넉백 당할때 isGrounded가 false 되는것 같음...
		else if (characterController.isGrounded == false && jumping == false)
		{
			// 계단에 안닿고 있을때만 Fall을 true
			if (Physics.Raycast(transform.position, -transform.up, 1.2f, LayerMask.GetMask("Stair")) == false && isKnockbacked == false)
			{
				Debug.DrawRay(transform.position, -transform.up * 1.2f, Color.red);
				//print("언제 호출됨2?");
				animator.SetBool("Fall", true);
			}
		}
	}

	[PunRPC]
	private void PlayRunningSoundRPC()
	{
		runAudioSource.clip = AudioManager.instance.runningClip;
		runAudioSource.volume = AudioManager.instance.audioValue;
		runAudioSource.Play();
	}

	[PunRPC]
	private void PlayJumpSoundRPC()
	{
		jumpAudioSource.clip = AudioManager.instance.jumpingClip;
		jumpAudioSource.volume = AudioManager.instance.audioValue;
		jumpAudioSource.Play();
	}

	[PunRPC]
	private void PlayKnockBackSoundRPC()
	{
		if (this.gameObject.name == "Player0(Clone)")
		{
			knockBackAudioSource.clip = AudioManager.instance.knockBackClips[0];
			knockBackAudioSource.volume = AudioManager.instance.audioValue * 0.8f;
			knockBackAudioSource.Play();
		}
		else if (this.gameObject.name == "Player2(Clone)")
		{
			knockBackAudioSource.clip = AudioManager.instance.knockBackClips[1];
			knockBackAudioSource.volume = AudioManager.instance.audioValue * 0.7f;
			knockBackAudioSource.Play();
		}
	}

	[PunRPC]
	private void PlayKnockBackedSoundRPC()
	{
		if (this.gameObject.name == "Player0(Clone)")
		{
			knockBackedAudioSource.clip = AudioManager.instance.knockBackedClips[0];
			knockBackedAudioSource.volume = AudioManager.instance.audioValue * 0.8f;
			knockBackedAudioSource.Play();
		}
		else if (this.gameObject.name == "Player2(Clone)")
		{
			knockBackedAudioSource.clip = AudioManager.instance.knockBackedClips[1];
			knockBackedAudioSource.volume = AudioManager.instance.audioValue * 0.7f;
			knockBackedAudioSource.Play();
		}
	}

	private void RotateView()
	{
		mouseLook.LookRotation(transform, cam.transform);
	}

	private void ResizeFieldOfView()
	{
		cameraFieldOfViewControll = Input.GetAxis("Mouse ScrollWheel");

		if (cameraFieldOfViewControll > 0.0f)
		{
			if (cam.fieldOfView < m_MaxFieldOfView)
				cam.fieldOfView++;
		}
		else if (cameraFieldOfViewControll < 0.0f)
		{
			if (cam.fieldOfView > m_MinFieldOfView)
				cam.fieldOfView--;
		}
	}

	private void CheckCharacterMoveState()
	{
		if (!jump && !jumping && characterController.isGrounded)
		{
			// 땅에 닿아있을때
			jump = Input.GetButtonDown("Jump");

            if(jump == true)
            {
				print("스페이스바 눌러지나?");
                pv.RPC("JumpDustRPC",
                       PhotonTargets.All, transform.position, transform.rotation);
            }
        }
		else if (!doubleJump && /*jumpCount < 1*/jumping && jumpItem)
		{
			// 현재 점프 상태를 jumpCount로? -> jumping이라는 변수가 이미 있으므로 이걸 이용 && 더블점프 아이템 소유 && 더블점프 아직 안썼을때
			doubleJump = Input.GetButtonDown("Jump");
			
			chargeJump = Input.GetButton("Jump");
        }

		if (!previouslyGrounded && characterController.isGrounded)
		{
			// 한 프레임 이전에 점프 상태 && 현재 프레임에서는 땅에 닿아있을때
			moveDir.y = 0f;
			// 점프 상태가 아니므로 false
			jumping = false;

			pv.RPC("JumpDustRPC",
						PhotonTargets.All, transform.position, transform.rotation);
		}
		if (previouslyGrounded == false && characterController.isGrounded == true)
		{
			// 처음으로 땅에 닿으면
			// 착지 사운드 재생
			AudioManager.instance.PlayLandSound(jumpAudioSource);
		}
		if (!characterController.isGrounded && !jumping && previouslyGrounded)
		{
			// 땅에 안닿아있을때 && 점프 상태 아닐때 && 한 프레임 이전에 땅에 닿았을때 = 그냥 떨어질때
			moveDir.y = 0f;
			//print("이전프레임엔 땅, 지금은 땅 아님, 점프 중도 아님 -> 그냥 떨어질때");
		}
		if (!characterController.isGrounded && jumping)
		{
			// 땅에 안닿아있을때 && 점프 상태일때
			chargeJump = Input.GetButton("Jump");
		}
		previouslyGrounded = characterController.isGrounded;
	}

	private void MoveCharacter()
	{
		float speed;

		GetInput(out speed);

		Vector3 desiredMove = transform.forward * input.y + transform.right * input.x;

		// 아래의 코드 4줄은 최종지점에 도착해야만 속도가 0이 되는것 처럼 보이게 해줌
		RaycastHit hitInfo;
		Physics.SphereCast(transform.position, characterController.radius, Vector3.down, out hitInfo,
							characterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
		desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

		moveDir.x = desiredMove.x * speed;
		moveDir.z = desiredMove.z * speed;

		if (characterController.isGrounded)
		{
			//땅위에 가만히있거나 땅에 닿은채로 이동
			moveDir.y = -m_StickToGroundForce;

			if (jump)
			{
				// 점프 사운드 재생
				//AudioManager.instance.PlayJumpSound(jumpAudioSource);
				pv.RPC("PlayJumpSoundRPC", PhotonTargets.All);
				moveDir.y = m_JumpSpeed;
				jump = false;
				jumping = true;
				jumpCount = 0;
			}

			characterController.Move(moveDir * Time.fixedDeltaTime);
			synchronizeSpeed = new Vector3(moveDir.x, 0, moveDir.z);
			ptv.SetSynchronizedValues(synchronizeSpeed, 0);
		}
		else if (jumping && doubleJump)
		{
			//2단 점프
			moveDir.y = m_JumpSpeed;
			jumpCount++;
			jumpItem = false;
			pv.RPC("ParticleDestroyRPC", PhotonTargets.All, pv.viewID);

			characterController.Move(moveDir * Time.fixedDeltaTime);
			ptv.SetSynchronizedValues(moveDir, 0);

			doubleJump = false;
		}
		else
		{
			//점프, 점프+이동, 그냥떨어짐, 그냥떨어짐+이동
			moveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
			// m_MoveDir += -9.81 * ? * 0.02

			//차지점프 중(스페이스바 길게 누르고 있을때)일때
			if (chargeJump)
			{
				// GravityMultiplier를 점점 줄인다
				m_GravityMultiplier *= 0.98f;
				// 점프할때 GravityMultiplier 너무 줄이면 무중력에 가까워지니까 적당히 줄인다
				if (m_GravityMultiplier < initialGravityMultiplier * 0.5f)
				{
					m_GravityMultiplier = initialGravityMultiplier * 0.5f;
				}
				// 다시 땅으로 떨어지려할때 초기 GravityMultiplier로 되돌린다
				if (moveDir.y < 0)
				{
					m_GravityMultiplier = initialGravityMultiplier;
				}
			}
			else
			{
				m_GravityMultiplier = initialGravityMultiplier;
			}

			characterController.Move(moveDir * Time.fixedDeltaTime);
			ptv.SetSynchronizedValues(moveDir, 0);
		}

		// Interpolation이 Synchronize Values일때, Extrapolation은 없을때 아래 코드 사용하는건가?
		// ptv.SetSynchronizedValues(m_MoveDir, 0);
		// 위 코드 Extrapolation이 Synchronize Values 있을때도 사용해봤는데 잘됨
		// 첫번째 파라미터에 캐릭터의 모든 이동 경우의 대한 속도를 계산해서 넘겨줘야함
		// 두번째 파라미터는 아예 안적을순 없으니 float 기본값인 0이라도 넘겨줬음
	}

	private void GetInput(out float speed)
	{
		float horizontal = Input.GetAxisRaw("Horizontal");
		float vertical = Input.GetAxisRaw("Vertical");

		speed = m_WalkSpeed;

		input = new Vector2(horizontal, vertical);
	}

	private void CheckCameraBlindness()
	{
		// 카메라의 움직임에 따라 캐릭터와의 거리가 미세하게 차이나므로 계속 계산해줌
		// -0.5f는 상수이므로 공식이 필요할듯
		offsetCameraCharacter = Vector3.Distance(transform.position, cam.transform.position) - 0.5f;
		//Debug.DrawRay(cam.transform.position, cam.transform.forward * offsetCameraCharacter, Color.yellow);

		// Ray가 맵(기둥)에 부딪힐때 그 맵을 저장해두고 MeshRenderer를 잠시 꺼둠
		if (Physics.Raycast(cam.transform.position,
							cam.transform.forward,
							out unmovingMapHitInfo,
							offsetCameraCharacter,
							LayerMask.GetMask("Map")))
		{
			blindMap = unmovingMapHitInfo.collider.gameObject;
			MeshRenderer[] temps = unmovingMapHitInfo.collider.gameObject.GetComponentsInChildren<MeshRenderer>();
			for (int i = 0; i < temps.Length; i++)
			{
				temps[i].enabled = false;
			}
		}
		// Ray가 맵(기둥)에 더이상 부딪히지 않을때 저장해둔 맵의 MeshRenderer를 다시 켬
		else
		{
			if (blindMap != null)
			{
				//blindObject.GetComponent<MeshRenderer>().enabled = true;
				MeshRenderer[] temps = blindMap.GetComponentsInChildren<MeshRenderer>();
				for (int i = 0; i < temps.Length; i++)
				{
					temps[i].enabled = true;
				}
			}
		}
	}

	private void KnockBackAttack()
	{
		//  내 forward 방향 RayCast로 적 플레이어 감지 되면 모든 플레이어들에게 넉백RPC 보냄
		if (Physics.Raycast(m_RayStart.transform.position, transform.forward, out hit, m_RayDistance, LayerMask.GetMask("Player"))
			|| Physics.Raycast(m_RayStart.transform.position + transform.up * characterController.radius, transform.forward, out hit, m_RayDistance, LayerMask.GetMask("Player"))
			|| Physics.Raycast(m_RayStart.transform.position - transform.up * characterController.radius, transform.forward, out hit, m_RayDistance, LayerMask.GetMask("Player"))
			|| Physics.Raycast(m_RayStart.transform.position + transform.right * characterController.radius, transform.forward, out hit, m_RayDistance, LayerMask.GetMask("Player"))
			|| Physics.Raycast(m_RayStart.transform.position - transform.right * characterController.radius, transform.forward, out hit, m_RayDistance, LayerMask.GetMask("Player"))
			|| Physics.Raycast(m_RayStart.transform.position + transform.right * characterController.radius + transform.up * characterController.radius, transform.forward, out hit, m_RayDistance, LayerMask.GetMask("Player"))
			|| Physics.Raycast(m_RayStart.transform.position - transform.right * characterController.radius + transform.up * characterController.radius, transform.forward, out hit, m_RayDistance, LayerMask.GetMask("Player"))
			|| Physics.Raycast(m_RayStart.transform.position + transform.right * characterController.radius - transform.up * characterController.radius, transform.forward, out hit, m_RayDistance, LayerMask.GetMask("Player"))
			|| Physics.Raycast(m_RayStart.transform.position - transform.right * characterController.radius - transform.up * characterController.radius, transform.forward, out hit, m_RayDistance, LayerMask.GetMask("Player"))
			)
		{
			// 모든 플레이어들에게 공격한 사람의 공격방향, 공격당한 사람의 고유viewID를 넘겨줌
			pv.RPC("KnockBackRPC", PhotonTargets.All, transform.forward, hit.transform.gameObject.GetComponent<PhotonView>().viewID);
		}
	}

	[PunRPC]
	private void KnockBackRPC(Vector3 dir, int id)
	{
		// 공격당한 사람에게만 넉백당할 방향을 알려줌
		PhotonView.Find(id).gameObject.GetComponent<Controller>().knockBackDir = dir;
		// 공격당한 사람만 넉백당할 권한을 줌
		PhotonView.Find(id).gameObject.GetComponent<Controller>().isKnockbacked = true;

        GameObject ticle = Instantiate(particleWave, PhotonView.Find(id).gameObject.transform.position +
            transform.right * 0.3f + 
            Vector3.up * 0.9f,
            Quaternion.identity);
        Destroy(ticle, 1.5f);
    }

	private void KnockBackAttacked()
	{
		print("반복");
		knockBackPower *= 0.98f;
		// 공격 당한 플레이어의 isKnockbacked값이 true로 바뀌기 때문에 공격 당한 플레이어만 적용

		// 공격 당한 플레이어는 knockBackDir 방향으로 넉백당함
		characterController.Move(knockBackDir * knockBackPower * Time.fixedDeltaTime);
		print(knockBackPower);
		if (characterController.isGrounded == true) // 땅에 닿아있을 경우
		{
			Vector3 temp = new Vector3(moveDir.x, 0, moveDir.z);
			// 땅에 닿아있을 경우의 Vector3와 넉백 Vector3를 합해주고 싱크
			synchronizeSpeed = temp + (knockBackDir * knockBackPower);
		}
		else // 땅에 안닿아있을 경우 = 공중에 떠있을 경우, m_CharacterController.isGrounded == false
		{
			// 공중에 떠있을 경우의 Vector3와 넉백 Vector3를 합해주고 싱크
			synchronizeSpeed = moveDir + (knockBackDir * knockBackPower);
		}
		// 허공에서 허공으로 넉백 당할 경우
		ptv.SetSynchronizedValues(synchronizeSpeed, 0);
		StartCoroutine(KnockBackEnd(1.0f));

        animator.SetBool("Attacked", true);
		StartCoroutine(SetBoolAfter("Attacked", false, 0.9f)); // 상수 넘겨주는거 안좋을듯, 해당 애니메이션의 진행도 얻어와서 생각해보기
	}

	IEnumerator SetBoolAfter(string name, bool value, float waitTime)
	{
		yield return new WaitForSeconds(waitTime);
		animator.SetBool(name, value);
	}

	private IEnumerator KnockBackEnd(float waitTime)
	{
		// waitTime 뒤에 넉백당할 권한 꺼줌 & knockBackPower를 초기상태로 되돌림
		yield return new WaitForSeconds(waitTime);
		isKnockbacked = false;
		knockBackPower = initialKnockBackPower;
		print("knockBackPower : " + knockBackPower);
	}

	private void CheckMovingMap()
	{
		// 캐릭터 중심축(발밑)에서 아래로 Ray 쏴서 움직이는맵 감지
		if (Physics.Raycast(transform.position, -Vector3.up, out movingMapHitInfo, 0.01f, LayerMask.GetMask("MovingMap")))
		{
			// Ray 테스트용
			//Debug.DrawRay(transform.position, -Vector3.up * 0.01f, Color.blue);
			// 움직이는맵 감지되면 그 맵의 자식으로 내 캐릭터를 넣음
			transform.parent = movingMapHitInfo.transform;
			// 나머지 플레이어들도 움직이는 맵과 그 맵을 밟은 플레이어 간의 부모-자식 관계 설정
			//print(movingMapHitInfo.transform.gameObject);
			if (previouslyGrounded == false && characterController.isGrounded == true)
			{
				//print("닿을때 한번?");
				pv.RPC("SetPlayerRelationOnMovingMapRPC",
						PhotonTargets.Others,
						movingMapHitInfo.transform.gameObject.GetComponentInParent<PhotonView>().viewID,
						pv.viewID);
			}
		}
		else
		{
			// 캐릭터의 부모를 다시 월드로해서 부모-자식 관계 해제
			transform.parent = null;
			// 나머지 플레이어들도 움직이는 맵과 그 맵을 밟았던 플레이어 간의 부모-자식 관계 해제
			if (previouslyGrounded == true && characterController.isGrounded == false)
			{
				//print("떨어질때 한번?");
				pv.RPC("ResetPlayerRelationRPC",
						PhotonTargets.Others,
						pv.viewID);
			}
		}
	}

	[PunRPC]
	private void SetPlayerRelationOnMovingMapRPC(int movingMapId, int playerId)
	{
		PhotonView.Find(playerId).gameObject.transform.parent = PhotonView.Find(movingMapId).gameObject.transform;
	}

    [PunRPC]
    private void JumpDustRPC(Vector3 pos, Quaternion rot)
    {
        GameObject ticle = Instantiate(particleJump, pos , rot * Quaternion.AngleAxis(180.0f, Vector3.up));
        Destroy(ticle, 1);
    }

	[PunRPC]
	private void ResetPlayerRelationRPC(int playerId)
	{
		PhotonView.Find(playerId).gameObject.transform.parent = null;
	}

	// 이 콜백메소드가 게임오브젝트에 바인딩된 스크립트에 있고
	// 그 게임오브젝트가 PhotonNetwork.Instantiate()에 의해 생성될때
	// 누구한테 호출되는거지?
	//public void OnPhotonInstantiate(PhotonMessageInfo info)
	//{
	//	print("누구한테 뿌리는거지?");
	//	print(info.ToString());
	//	print(info.sender.ToString());
	//	//print(info.photonView.gameObject.name);
	//}

	private void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.isWriting)
		{
			stream.SendNext(animator.GetBool("Run"));
			stream.SendNext(animator.GetBool("Jump"));
			stream.SendNext(animator.GetBool("Attack"));
			stream.SendNext(animator.GetBool("Attacked"));
			stream.SendNext(animator.GetBool("Fall"));

		}
		else
		{
			animator.SetBool("Run", (bool)stream.ReceiveNext());
			animator.SetBool("Jump", (bool)stream.ReceiveNext());
			animator.SetBool("Attack", (bool)stream.ReceiveNext());
			animator.SetBool("Attacked", (bool)stream.ReceiveNext());
			animator.SetBool("Fall", (bool)stream.ReceiveNext());
		}
	}
}


