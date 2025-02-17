using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
#endif

///TODO
// Better implement the new input system.
// create compatibility layers for Unity 2017 and 2018
// better implement animation calls(?)
// more camera animations
namespace SUPERCharacte
{
    [RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))]
    [AddComponentMenu("SUPER Character/JuanMove")]
    public class JuanMoveBehaviour : MonoBehaviour
    {
        #region Variables
        public Controller controller;
        public GameObject posCamera;
        public bool cubriendose;
        public bool atacando;
        public bool tengoFacon;
        public bool tengoRifle;
        public bool tengoBoleadoras;
        public bool controllerPaused = false;
        public Vector3 ultimaPosicion;

        public bool estaMuerto;
        public bool murio;

        #region Movimiento2
        public float speed = 5f;
        public float speedRotate = 200f;
        public float horizontal;
        public float vertical;
        #endregion

        #region Camera Settings
        [Header("Camera Settings")]
        //
        //Public
        //
        //Both
        public Camera playerCamera;
        public bool enableCameraControl = true, lockAndHideMouse = true, autoGenerateCrosshair = true, showCrosshairIn3rdPerson = false, drawPrimitiveUI = false;
        public Sprite crosshairSprite;
        public PerspectiveModes cameraPerspective = PerspectiveModes._3rdPerson;
        //use mouse wheel to switch modes. (too close will set it to fps mode and attempting to zoom out from fps will switch to tps mode)
        public bool automaticallySwitchPerspective = true;
/*#if ENABLE_INPUT_SYSTEM
    public Key perspectiveSwitchingKey = Key.Q;
#else
        public KeyCode perspectiveSwitchingKey_L = KeyCode.None;
#endif*/

        public MouseInputInversionModes mouseInputInversion;
        public float Sensitivity = 8;
        public float rotationWeight = 4;
        public float verticalRotationRange = 170.0f;
        public float standingEyeHeight = 0.8f;
        public float crouchingEyeHeight = 0.25f;

        //First person
        public ViewInputModes viewInputMethods;
        public float FOVKickAmount = 10;
        public float FOVSensitivityMultiplier = 0.74f;

        //Third Person
        public bool rotateCharacterToCameraForward = false;
        public float maxCameraDistance = 8;
        public LayerMask cameraObstructionIgnore = -1;
        public float cameraZoomSensitivity = 5;
        public float bodyCatchupSpeed = 2.5f;
        public float inputResponseFiltering = 2.5f;



        //
        //Internal
        //

        //Both
        Vector2 MouseXY;
        Vector2 viewRotVelRef;
        bool isInFirstPerson, isInThirdPerson, perspecTog;
        bool setInitialRot = true;
        Vector3 initialRot;
        Image crosshairImg;
        Image stamMeter, stamMeterBG;
        Image statsPanel, statsPanelBG;
        Image HealthMeter, HydrationMeter, HungerMeter;
        Vector2 normalMeterSizeDelta = new Vector2(175, 12), normalStamMeterSizeDelta = new Vector2(330, 5);
        float internalEyeHeight;

        //First Person
        float initialCameraFOV, FOVKickVelRef, currentFOVMod;

        //Third Person
        float mouseScrollWheel, maxCameraDistInternal, currentCameraZ, cameraZRef;
        Vector3 headPos, headRot, currentCameraPos, cameraPosVelRef;
        Quaternion quatHeadRot;
        Ray cameraObstCheck;
        RaycastHit cameraObstResult;
        [Space(20)]
        #endregion

        #region Movement
        [Header("Movement Settings")]

        //
        //Public
        //
        public bool enableMovementControl = true;

        //Walking/Sprinting/Crouching
        [Range(1.0f, 650.0f)] public float walkingSpeed = 140, sprintingSpeed = 260, crouchingSpeed = 45;
        [Range(1.0f, 400.0f)] public float decelerationSpeed = 240;
#if ENABLE_INPUT_SYSTEM
    public Key sprintKey = Key.LeftShift, crouchKey = Key.LeftCtrl, slideKey = Key.V;
#else
        public KeyCode sprintKey_L = KeyCode.LeftShift, crouchKey_L = KeyCode.LeftControl, slideKey_L = KeyCode.V;
#endif
        public bool canSprint = true, isSprinting, toggleSprint, sprintOverride, canCrouch = true, isCrouching, toggleCrouch, crouchOverride, isIdle;
        public Stances currentStance = Stances.Standing;
        public float stanceTransitionSpeed = 5.0f, crouchingHeight = 0.80f;
        public GroundSpeedProfiles currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
        public LayerMask whatIsGround = -1;

        //Slope affectors
        public float hardSlopeLimit = 70, slopeInfluenceOnSpeed = 1, maxStairRise = 0.25f, stepUpSpeed = 0.2f;

        //Jumping
        public bool canJump = true, holdJump = false, jumpEnhancements = true, Jumped;
#if ENABLE_INPUT_SYSTEM
        public Key jumpKey = Key.Space;
#else
        public KeyCode jumpKey_L = KeyCode.Space;
#endif
        [Range(1.0f, 650.0f)] public float jumpPower = 40;
        [Range(0.0f, 1.0f)] public float airControlFactor = 1;
        public float decentMultiplier = 2.5f, tapJumpMultiplier = 2.1f;
        float jumpBlankingPeriod;

        //Sliding
        public bool isSliding, canSlide = true;
        public float slidingDeceleration = 150.0f, slidingTransitionSpeed = 4, maxFlatSlideDistance = 10;


        //
        //Internal
        //

        //Walking/Sprinting/Crouching
        public GroundInfo currentGroundInfo = new GroundInfo();
        float standingHeight;
        float currentGroundSpeed;
        Vector3 InputDir;
        float HeadRotDirForInput;
        Vector2 MovInput;
        Vector2 MovInput_Smoothed;
        Vector2 _2DVelocity;
        float _2DVelocityMag, speedToVelocityRatio;
        PhysicMaterial _ZeroFriction, _MaxFriction;
        CapsuleCollider capsule;
        public Rigidbody p_Rigidbody;
        bool crouchInput_Momentary, crouchInput_FrameOf, sprintInput_FrameOf, sprintInput_Momentary, slideInput_FrameOf, slideInput_Momentary;
        bool changingStances = false;

        //Slope Affectors

        //Jumping
        bool jumpInput_Momentary, jumpInput_FrameOf;

        //Sliding
        Vector3 cachedDirPreSlide, cachedPosPreSlide;



        [Space(20)]
        #endregion

        #region Stamina System
        //Public
        public bool enableStaminaSystem = true, jumpingDepletesStamina = true;
        [Range(0.0f, 250.0f)] public float Stamina = 50.0f, currentStaminaLevel = 0, s_minimumStaminaToSprint = 5.0f, s_depletionSpeed = 2.0f, s_regenerationSpeed = 1.2f, s_JumpStaminaDepletion = 5.0f, s_FacaStaminaDepletion = 2.0f;

        //Internal
        bool staminaIsChanging;
        bool ignoreStamina = false;
        #endregion

        #region Footstep System
        [Header("Footstep System")]
        public bool enableFootstepSounds = true;
        public FootstepTriggeringMode footstepTriggeringMode = FootstepTriggeringMode.calculatedTiming;
        [Range(0.0f, 1.0f)] public float stepTiming = 0.15f;
        [Range(0.0f, 1.0f)] public float modificadorCorriendo = 0.50f;
        public List<GroundMaterialProfile> footstepSoundSet = new List<GroundMaterialProfile>();
        bool shouldCalculateFootstepTriggers = true;
        float StepCycle = 0;
        AudioSource playerAudioSource;
        List<AudioClip> currentClipSet = new List<AudioClip>();
        [Space(18)]
        #endregion


        #region Collectables
        #endregion

        #region Animation
        //
        //Pulbic
        //

        //Firstperson
        //public Animator _1stPersonCharacterAnimator;
        //ThirdPerson
        public Animator _3rdPersonCharacterAnimator;
        public string a_velocity, a_2DVelocity, a_Grounded, a_Idle, a_Jumped,
            a_Sliding, a_Sprinting, a_Crouching, a_facon, a_faconazo, a_esquivar,
            a_poncho, a_velXZ, a_rifle, a_boleadoras, a_VelX, a_VelY, a_lanzar, a_isDeath;
        public bool stickRendererToCapsuleBottom = true;
        public Vector3 velXZ;

        #endregion

        [Space(18)]
        public bool enableGroundingDebugging = false, enableMovementDebugging = false, enableMouseAndCameraDebugging = false, enableVaultDebugging = false;
        #endregion
        void Start()
        {

            tengoFacon = false;
            tengoRifle = false;
            tengoBoleadoras = false;
            cubriendose = false;
            estaMuerto = false;
            murio = false;



            #region Camera
            maxCameraDistInternal = maxCameraDistance;
            initialCameraFOV = playerCamera.fieldOfView;
            internalEyeHeight = standingEyeHeight;
            if (lockAndHideMouse)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (autoGenerateCrosshair || drawPrimitiveUI)
            {
                Canvas canvas = playerCamera.gameObject.GetComponentInChildren<Canvas>();
                if (canvas == null) { canvas = new GameObject("AutoCrosshair").AddComponent<Canvas>(); }
                canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = true;
                canvas.transform.SetParent(playerCamera.transform);
                canvas.transform.position = Vector3.zero;
                if (autoGenerateCrosshair && crosshairSprite)
                {
                    crosshairImg = new GameObject("Crosshair").AddComponent<Image>();
                    crosshairImg.sprite = crosshairSprite;
                    crosshairImg.rectTransform.sizeDelta = new Vector2(25, 25);
                    crosshairImg.transform.SetParent(canvas.transform);
                    crosshairImg.transform.position = Vector3.zero;
                    crosshairImg.raycastTarget = false;
                }
                if (drawPrimitiveUI)
                {
                    //Stam Meter BG
                    stamMeterBG = new GameObject("Stam BG").AddComponent<Image>();
                    stamMeterBG.rectTransform.sizeDelta = normalStamMeterSizeDelta;
                    stamMeterBG.transform.SetParent(canvas.transform);
                    stamMeterBG.rectTransform.anchorMin = new Vector2(0.5f, 0);
                    stamMeterBG.rectTransform.anchorMax = new Vector2(0.5f, 0);
                    stamMeterBG.rectTransform.anchoredPosition = new Vector2(0, 22);
                    stamMeterBG.color = Color.gray;
                    stamMeterBG.gameObject.SetActive(enableStaminaSystem);
                    //Stam Meter
                    stamMeter = new GameObject("Stam Meter").AddComponent<Image>();
                    stamMeter.rectTransform.sizeDelta = normalStamMeterSizeDelta;
                    stamMeter.transform.SetParent(canvas.transform);
                    stamMeter.rectTransform.anchorMin = new Vector2(0.5f, 0);
                    stamMeter.rectTransform.anchorMax = new Vector2(0.5f, 0);
                    stamMeter.rectTransform.anchoredPosition = new Vector2(0, 22);
                    stamMeter.color = Color.white;
                    stamMeter.gameObject.SetActive(enableStaminaSystem);

                }
            }
            initialRot = transform.localEulerAngles;
            #endregion

            #region Movement
            p_Rigidbody = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            standingHeight = capsule.height;
            currentGroundSpeed = walkingSpeed;
            _ZeroFriction = new PhysicMaterial("Zero_Friction");
            _ZeroFriction.dynamicFriction = 0f;
            _ZeroFriction.staticFriction = 0;
            _ZeroFriction.frictionCombine = PhysicMaterialCombine.Minimum;
            _ZeroFriction.bounceCombine = PhysicMaterialCombine.Minimum;
            _MaxFriction = new PhysicMaterial("Max_Friction");
            _MaxFriction.dynamicFriction = 1;
            _MaxFriction.staticFriction = 1;
            _MaxFriction.frictionCombine = PhysicMaterialCombine.Maximum;
            _MaxFriction.bounceCombine = PhysicMaterialCombine.Average;
            #endregion

            #region Stamina System
            currentStaminaLevel = Stamina;
            #endregion

            #region Footstep
            playerAudioSource = GetComponent<AudioSource>();
            #endregion

        }
        void Update()
        {
            if (!estaMuerto)
            {
                if (!controllerPaused)
                {
                    //EquiparFacon();
                    #region Input
                    /*#if ENABLE_INPUT_SYSTEM
                                MouseXY.x = Mouse.current.delta.y.ReadValue()/50;
                                MouseXY.y = Mouse.current.delta.x.ReadValue()/50;

                                mouseScrollWheel = Mouse.current.scroll.y.ReadValue()/1000;
                                if(perspectiveSwitchingKey!=Key.None)perspecTog = Keyboard.current[perspectiveSwitchingKey].wasPressedThisFrame;
                                if(interactKey!=Key.None)interactInput = Keyboard.current[interactKey].wasPressedThisFrame;
                                //movement

                                 if(jumpKey!=Key.None)jumpInput_Momentary =  Keyboard.current[jumpKey].isPressed;
                                 if(jumpKey!=Key.None)jumpInput_FrameOf =  Keyboard.current[jumpKey].wasPressedThisFrame;

                                 if(crouchKey!=Key.None){
                                    crouchInput_Momentary =  Keyboard.current[crouchKey].isPressed;
                                    crouchInput_FrameOf = Keyboard.current[crouchKey].wasPressedThisFrame;
                                 }
                                 if(sprintKey!=Key.None){
                                    sprintInput_Momentary = Keyboard.current[sprintKey].isPressed;
                                    sprintInput_FrameOf = Keyboard.current[sprintKey].wasPressedThisFrame;
                                 }
                                 if(slideKey != Key.None){
                                    slideInput_Momentary = Keyboard.current[slideKey].isPressed;
                                    slideInput_FrameOf = Keyboard.current[slideKey].wasPressedThisFrame;
                                 }
                    #if SAIO_ENABLE_PARKOUR
                                vaultInput = Keyboard.current[VaultKey].isPressed;
                    #endif
                                MovInput.x = Keyboard.current.aKey.isPressed ? -1 : Keyboard.current.dKey.isPressed ? 1 : 0;
                                MovInput.y = Keyboard.current.wKey.isPressed ? 1 : Keyboard.current.sKey.isPressed ? -1 : 0;
                    #else */
                    //camera
                    MouseXY.x = Input.GetAxis("Mouse Y");
                    MouseXY.y = Input.GetAxis("Mouse X");
                    mouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");
                    //perspecTog = Input.GetKeyDown(perspectiveSwitchingKey_L);
                    //interactInput = Input.GetKeyDown(interactKey_L);
                    //movement

                    jumpInput_Momentary = Input.GetKey(jumpKey_L);
                    jumpInput_FrameOf = Input.GetKeyDown(jumpKey_L);
                    crouchInput_Momentary = Input.GetKey(crouchKey_L);
                    crouchInput_FrameOf = Input.GetKeyDown(crouchKey_L);
                    sprintInput_Momentary = Input.GetKey(sprintKey_L);
                    sprintInput_FrameOf = Input.GetKeyDown(sprintKey_L);
                    slideInput_Momentary = Input.GetKey(slideKey_L);
                    slideInput_FrameOf = Input.GetKeyDown(slideKey_L);
#if SAIO_ENABLE_PARKOUR

            vaultInput = Input.GetKeyDown(VaultKey_L);
#endif
                    MovInput = Vector2.up * Input.GetAxisRaw("Vertical") + Vector2.right * Input.GetAxisRaw("Horizontal");
                    //#endif
                    #endregion

                    if (!tengoBoleadoras)
                    {
                        #region Camera
                        if (enableCameraControl)
                        {
                            switch (cameraPerspective)
                            {
                                case PerspectiveModes._1stPerson:
                                    {
                                        //This is called in FixedUpdate for the 3rd person mode
                                        //RotateView(MouseXY, Sensitivity, rotationWeight);
                                        if (!isInFirstPerson) { ChangePerspective(PerspectiveModes._1stPerson); }
                                        if (perspecTog || (automaticallySwitchPerspective && mouseScrollWheel < 0)) { ChangePerspective(PerspectiveModes._3rdPerson); }
                                        //HeadbobCycleCalculator();
                                        FOVKick();
                                    }
                                    break;

                                case PerspectiveModes._3rdPerson:
                                    {
                                        //  UpdateCameraPosition_3rdPerson();
                                        if (!isInThirdPerson) { ChangePerspective(PerspectiveModes._3rdPerson); }
                                        if (perspecTog || (automaticallySwitchPerspective && maxCameraDistInternal == 0 && currentCameraZ == 0)) { ChangePerspective(PerspectiveModes._1stPerson); }
                                        maxCameraDistInternal = Mathf.Clamp(maxCameraDistInternal - (mouseScrollWheel * (cameraZoomSensitivity * 2)), automaticallySwitchPerspective ? 0 : (capsule.radius * 2), maxCameraDistance);
                                    }
                                    break;
                            }


                            if (setInitialRot)
                            {
                                setInitialRot = false;
                                RotateView(initialRot, false);
                                InputDir = transform.forward;
                            }
                        }
                        if (drawPrimitiveUI)
                        {
                            /* if (enableSurvivalStats)
                             {
                                 if (!statsPanel.gameObject.activeSelf) statsPanel.gameObject.SetActive(true);

                                 HealthMeter.rectTransform.sizeDelta = Vector2.Lerp(Vector2.up * 12, normalMeterSizeDelta, (currentSurvivalStats.Health / defaultSurvivalStats.Health));
                                 HydrationMeter.rectTransform.sizeDelta = Vector2.Lerp(Vector2.up * 12, normalMeterSizeDelta, (currentSurvivalStats.Hydration / defaultSurvivalStats.Hydration));
                                 HungerMeter.rectTransform.sizeDelta = Vector2.Lerp(Vector2.up * 12, normalMeterSizeDelta, (currentSurvivalStats.Hunger / defaultSurvivalStats.Hunger));
                             }
                             else
                             {
                                 if (statsPanel.gameObject.activeSelf) statsPanel.gameObject.SetActive(false);

                             } */
                            if (enableStaminaSystem)
                            {
                                if (!stamMeterBG.gameObject.activeSelf) stamMeterBG.gameObject.SetActive(true);
                                if (!stamMeter.gameObject.activeSelf) stamMeter.gameObject.SetActive(true);
                                if (staminaIsChanging)
                                {
                                    if (stamMeter.color != Color.white)
                                    {
                                        stamMeterBG.color = Vector4.MoveTowards(stamMeterBG.color, new Vector4(0, 0, 0, 0.5f), 0.15f);
                                        stamMeter.color = Vector4.MoveTowards(stamMeter.color, new Vector4(1, 1, 1, 1), 0.15f);
                                    }
                                    stamMeter.rectTransform.sizeDelta = Vector2.Lerp(Vector2.up * 5, normalStamMeterSizeDelta, (currentStaminaLevel / Stamina));
                                }
                                else
                                {
                                    if (stamMeter.color != Color.clear)
                                    {
                                        stamMeterBG.color = Vector4.MoveTowards(stamMeterBG.color, new Vector4(0, 0, 0, 0), 0.15f);
                                        stamMeter.color = Vector4.MoveTowards(stamMeter.color, new Vector4(0, 0, 0, 0), 0.15f);
                                    }
                                }
                            }
                            else
                            {
                                if (stamMeterBG.gameObject.activeSelf) stamMeterBG.gameObject.SetActive(false);
                                if (stamMeter.gameObject.activeSelf) stamMeter.gameObject.SetActive(false);
                            }
                        }

                        if (currentStance == Stances.Standing && !changingStances)
                        {
                            internalEyeHeight = standingEyeHeight;
                        }
                        #endregion

                        //if(Input.GetKeyDown(KeyCode.Mouse0))
                        if (!atacando && !cubriendose)
                        {
                            #region Movement
                            if (cameraPerspective == PerspectiveModes._3rdPerson && !atacando)
                            {
                                HeadRotDirForInput = Mathf.MoveTowardsAngle(HeadRotDirForInput, headRot.y, bodyCatchupSpeed * (1 + Time.deltaTime));
                                MovInput_Smoothed = Vector2.MoveTowards(MovInput_Smoothed, MovInput, inputResponseFiltering * (1 + Time.deltaTime));
                            }
                            InputDir = cameraPerspective == PerspectiveModes._1stPerson ? Vector3.ClampMagnitude((transform.forward * MovInput.y + transform.right * (viewInputMethods == ViewInputModes.Traditional ? MovInput.x : 0)), 1) : Quaternion.AngleAxis(HeadRotDirForInput, Vector3.up) * (Vector3.ClampMagnitude((Vector3.forward * MovInput_Smoothed.y + Vector3.right * MovInput_Smoothed.x), 1));
                            GroundMovementSpeedUpdate();
                            if (canJump && !tengoFacon && !tengoRifle && !tengoBoleadoras && (holdJump ? jumpInput_Momentary : jumpInput_FrameOf)) { Jump(jumpPower); }
                            #endregion
                            #region Footstep
                            CalculateFootstepTriggers();
                            #endregion
                        }
                    }



                    #region Stamina system
                    if (enableStaminaSystem) { CalculateStamina(); }
                    #endregion



                }
                else
                {
                    jumpInput_FrameOf = false;
                    jumpInput_Momentary = false;
                }
            }
            
            #region Animation
            UpdateAnimationTriggers(controllerPaused);
            #endregion
        }
        void FixedUpdate()
        {
            if (!controllerPaused && !estaMuerto)
            {
                if (!tengoBoleadoras)
                {
                    if (!atacando && !cubriendose)
                    {
                        #region Movement
                        if (enableMovementControl)
                        {
                            GetGroundInfo();
                            MovePlayer(InputDir, currentGroundSpeed);
                            velXZ = new Vector3(p_Rigidbody.velocity.x, 0, p_Rigidbody.velocity.z);

                            //Debug.Log(velXZ.magnitude);

                            // if (isSliding) { Slide(); }
                        }
                        #endregion

                    }

                    #region Camera
                    RotateView(MouseXY, Sensitivity, rotationWeight);
                    if (cameraPerspective == PerspectiveModes._3rdPerson)
                    {
                        UpdateBodyRotation_3rdPerson();
                        UpdateCameraPosition_3rdPerson();
                    }

                    #endregion
                }
                else
                {
                    MovePlayer();
                    //transform.forward = Camera.main.transform.forward;
                }

            }
        }

        void DejaDeGolpear()
        {
            atacando = false;
        }

        void EquiparFacon()
        {

        }
        #region Movement2
        public void MovePlayer()
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
            transform.Translate(new Vector3(horizontal, 0.0f, vertical) * Time.deltaTime * speed);

            float rotationY = (Input.GetAxis("Mouse X"));
            transform.Rotate(new Vector3(0, rotationY, 0) * Time.deltaTime * speedRotate);
            

        }
        #endregion

        /*   private void OnTriggerEnter(Collider other)
           {
               #region Collectables
               other.GetComponent<ICollectable>()?.Collect();
               #endregion
           }*/

        #region Camera Functions
        void RotateView(Vector2 yawPitchInput, float inputSensitivity, float cameraWeight)
        {

            switch (viewInputMethods)
            {

                case ViewInputModes.Traditional:
                    {
                        yawPitchInput.x *= ((mouseInputInversion == MouseInputInversionModes.X || mouseInputInversion == MouseInputInversionModes.Both) ? 1 : -1);
                        yawPitchInput.y *= ((mouseInputInversion == MouseInputInversionModes.Y || mouseInputInversion == MouseInputInversionModes.Both) ? -1 : 1);
                        float maxDelta = Mathf.Min(5, (26 - cameraWeight)) * 360;
                        switch (cameraPerspective)
                        {
                            case PerspectiveModes._1stPerson:
                                {
                                    Vector2 targetAngles = ((Vector2.right * playerCamera.transform.localEulerAngles.x) + (Vector2.up * p_Rigidbody.rotation.eulerAngles.y));
                                    float fovMod = FOVSensitivityMultiplier > 0 && playerCamera.fieldOfView <= initialCameraFOV ? ((initialCameraFOV - playerCamera.fieldOfView) * (FOVSensitivityMultiplier / 10)) + 1 : 1;
                                    targetAngles = Vector2.SmoothDamp(targetAngles, targetAngles + (yawPitchInput * (((inputSensitivity * 5) / fovMod))), ref viewRotVelRef, (Mathf.Pow(cameraWeight * fovMod, 2)) * Time.fixedDeltaTime, maxDelta, Time.fixedDeltaTime);

                                    targetAngles.x += targetAngles.x > 180 ? -360 : targetAngles.x < -180 ? 360 : 0;
                                    targetAngles.x = Mathf.Clamp(targetAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
                                    //playerCamera.transform.localEulerAngles = (Vector3.right * targetAngles.x) + (Vector3.forward * (enableHeadbob ? headbobCameraPosition.z : 0));
                                    playerCamera.transform.localEulerAngles = (Vector3.right * targetAngles.x) + (Vector3.forward);
                                    p_Rigidbody.MoveRotation(Quaternion.Euler(Vector3.up * targetAngles.y));

                                    //p_Rigidbody.rotation = ;
                                    //transform.localEulerAngles = (Vector3.up*targetAngles.y);
                                }
                                break;

                            case PerspectiveModes._3rdPerson:
                                {

                                    headPos = transform.position + Vector3.up * standingEyeHeight;
                                    quatHeadRot = Quaternion.Euler(headRot);
                                    headRot = Vector3.SmoothDamp(headRot, headRot + ((Vector3)yawPitchInput * (inputSensitivity * 5)), ref cameraPosVelRef, (Mathf.Pow(cameraWeight, 2)) * Time.fixedDeltaTime, maxDelta, Time.fixedDeltaTime);
                                    headRot.y += headRot.y > 180 ? -360 : headRot.y < -180 ? 360 : 0;
                                    headRot.x += headRot.x > 180 ? -360 : headRot.x < -180 ? 360 : 0;
                                    headRot.x = Mathf.Clamp(headRot.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);


                                }
                                break;

                        }

                    }
                    break;

                case ViewInputModes.Retro:
                    {
                        yawPitchInput = Vector2.up * (Input.GetAxis("Horizontal") * ((mouseInputInversion == MouseInputInversionModes.Y || mouseInputInversion == MouseInputInversionModes.Both) ? -1 : 1));
                        Vector2 targetAngles = ((Vector2.right * playerCamera.transform.localEulerAngles.x) + (Vector2.up * transform.localEulerAngles.y));
                        float fovMod = FOVSensitivityMultiplier > 0 && playerCamera.fieldOfView <= initialCameraFOV ? ((initialCameraFOV - playerCamera.fieldOfView) * (FOVSensitivityMultiplier / 10)) + 1 : 1;
                        targetAngles = targetAngles + (yawPitchInput * ((inputSensitivity / fovMod)));
                        targetAngles.x = 0;
                        //playerCamera.transform.localEulerAngles = (Vector3.right * targetAngles.x) + (Vector3.forward * (enableHeadbob ? headbobCameraPosition.z : 0));
                        playerCamera.transform.localEulerAngles = (Vector3.right * targetAngles.x) + (Vector3.forward);
                        transform.localEulerAngles = (Vector3.up * targetAngles.y);
                    }
                    break;
            }

        }
        public void RotateView(Vector3 AbsoluteEulerAngles, bool SmoothRotation)
        {

            switch (cameraPerspective)
            {

                case (PerspectiveModes._1stPerson):
                    {
                        AbsoluteEulerAngles.x += AbsoluteEulerAngles.x > 180 ? -360 : AbsoluteEulerAngles.x < -180 ? 360 : 0;
                        AbsoluteEulerAngles.x = Mathf.Clamp(AbsoluteEulerAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);


                        if (SmoothRotation)
                        {
                            IEnumerator SmoothRot()
                            {
                                //doingCamInterp = true;
                                Vector3 refVec = Vector3.zero, targetAngles = (Vector3.right * playerCamera.transform.localEulerAngles.x) + Vector3.up * transform.eulerAngles.y;
                                while (Vector3.Distance(targetAngles, AbsoluteEulerAngles) > 0.1f)
                                {
                                    targetAngles = Vector3.SmoothDamp(targetAngles, AbsoluteEulerAngles, ref refVec, 25 * Time.deltaTime);
                                    targetAngles.x += targetAngles.x > 180 ? -360 : targetAngles.x < -180 ? 360 : 0;
                                    targetAngles.x = Mathf.Clamp(targetAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
                                    playerCamera.transform.localEulerAngles = Vector3.right * targetAngles.x;
                                    transform.eulerAngles = Vector3.up * targetAngles.y;
                                    yield return null;
                                }
                                //doingCamInterp = false;
                            }
                            StopCoroutine("SmoothRot");
                            StartCoroutine(SmoothRot());
                        }
                        else
                        {
                            playerCamera.transform.eulerAngles = Vector3.right * AbsoluteEulerAngles.x;
                            transform.eulerAngles = (Vector3.up * AbsoluteEulerAngles.y) + (Vector3.forward * AbsoluteEulerAngles.z);
                        }
                    }
                    break;

                case (PerspectiveModes._3rdPerson):
                    {
                        if (SmoothRotation)
                        {
                            AbsoluteEulerAngles.y += AbsoluteEulerAngles.y > 180 ? -360 : AbsoluteEulerAngles.y < -180 ? 360 : 0;
                            AbsoluteEulerAngles.x += AbsoluteEulerAngles.x > 180 ? -360 : AbsoluteEulerAngles.x < -180 ? 360 : 0;
                            AbsoluteEulerAngles.x = Mathf.Clamp(AbsoluteEulerAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
                            IEnumerator SmoothRot()
                            {
                                //doingCamInterp = true;
                                Vector3 refVec = Vector3.zero;
                                while (Vector3.Distance(headRot, AbsoluteEulerAngles) > 0.1f)
                                {
                                    headPos = p_Rigidbody.position + Vector3.up * standingEyeHeight;
                                    quatHeadRot = Quaternion.Euler(headRot);
                                    headRot = Vector3.SmoothDamp(headRot, AbsoluteEulerAngles, ref refVec, 25 * Time.deltaTime);
                                    headRot.y += headRot.y > 180 ? -360 : headRot.y < -180 ? 360 : 0;
                                    headRot.x += headRot.x > 180 ? -360 : headRot.x < -180 ? 360 : 0;
                                    headRot.x = Mathf.Clamp(headRot.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
                                    yield return null;
                                }
                                //doingCamInterp = false;
                            }
                            StopCoroutine("SmoothRot");
                            StartCoroutine(SmoothRot());
                        }
                        else
                        {
                            headRot = AbsoluteEulerAngles;
                            headRot.y += headRot.y > 180 ? -360 : headRot.y < -180 ? 360 : 0;
                            headRot.x += headRot.x > 180 ? -360 : headRot.x < -180 ? 360 : 0;
                            headRot.x = Mathf.Clamp(headRot.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
                            quatHeadRot = Quaternion.Euler(headRot);
                            //if (doingCamInterp) { }
                        }
                    }
                    break;
            }
        }
        public void ChangePerspective(PerspectiveModes newPerspective = PerspectiveModes._1stPerson)
        {
            switch (newPerspective)
            {
                case PerspectiveModes._1stPerson:
                    {
                        StopCoroutine("SmoothRot");
                        isInThirdPerson = false;
                        isInFirstPerson = true;
                        transform.eulerAngles = Vector3.up * headRot.y;
                        playerCamera.transform.localPosition = Vector3.up * standingEyeHeight;
                        playerCamera.transform.localEulerAngles = (Vector2)playerCamera.transform.localEulerAngles;
                        cameraPerspective = newPerspective;
                        if (_3rdPersonCharacterAnimator)
                        {
                            _3rdPersonCharacterAnimator.gameObject.SetActive(false);
                        }
                        /*if (_1stPersonCharacterAnimator)
                        {
                            _1stPersonCharacterAnimator.gameObject.SetActive(true);
                        }*/
                        if (crosshairImg && autoGenerateCrosshair)
                        {
                            crosshairImg.gameObject.SetActive(true);
                        }
                    }
                    break;

                case PerspectiveModes._3rdPerson:
                    {
                        StopCoroutine("SmoothRot");
                        isInThirdPerson = true;
                        isInFirstPerson = false;
                        playerCamera.fieldOfView = initialCameraFOV;
                        maxCameraDistInternal = maxCameraDistInternal == 0 ? capsule.radius * 2 : maxCameraDistInternal;
                        currentCameraZ = -(maxCameraDistInternal * 0.85f);
                        playerCamera.transform.localEulerAngles = (Vector2)playerCamera.transform.localEulerAngles;
                        headRot.y = transform.eulerAngles.y;
                        headRot.x = playerCamera.transform.eulerAngles.x;
                        cameraPerspective = newPerspective;
                        if (_3rdPersonCharacterAnimator)
                        {
                            _3rdPersonCharacterAnimator.gameObject.SetActive(true);
                        }
                        /*if (_1stPersonCharacterAnimator)
                        {
                            _1stPersonCharacterAnimator.gameObject.SetActive(false);
                        }*/
                        if (crosshairImg && autoGenerateCrosshair)
                        {
                            if (!showCrosshairIn3rdPerson)
                            {
                                crosshairImg.gameObject.SetActive(false);
                            }
                            else
                            {
                                crosshairImg.gameObject.SetActive(true);
                            }
                        }
                    }
                    break;
            }
        }
        void FOVKick()
        {
            if (cameraPerspective == PerspectiveModes._1stPerson && FOVKickAmount > 0)
            {
                currentFOVMod = (!isIdle && isSprinting) ? initialCameraFOV + (FOVKickAmount * ((sprintingSpeed / walkingSpeed) - 1)) : initialCameraFOV;
                if (!Mathf.Approximately(playerCamera.fieldOfView, currentFOVMod) && playerCamera.fieldOfView >= initialCameraFOV)
                {
                    playerCamera.fieldOfView = Mathf.SmoothDamp(playerCamera.fieldOfView, currentFOVMod, ref FOVKickVelRef, Time.deltaTime, 50);
                }
            }
        }
       /* void HeadbobCycleCalculator()
        {
            if (enableHeadbob)
            {
                if (!isIdle && currentGroundInfo.isGettingGroundInfo && !isSliding)
                {
                    headbobWarmUp = Mathf.MoveTowards(headbobWarmUp, 1, Time.deltaTime * 5);
                    headbobCyclePosition += (_2DVelocity.magnitude) * (Time.deltaTime * (headbobSpeed / 10));

                    headbobCameraPosition.x = (((Mathf.Sin(Mathf.PI * (2 * headbobCyclePosition + 0.5f))) * (headbobPower / 50))) * headbobWarmUp;
                    headbobCameraPosition.y = ((Mathf.Abs((((Mathf.Sin(Mathf.PI * (2 * headbobCyclePosition))) * 0.75f)) * (headbobPower / 50))) * headbobWarmUp) + internalEyeHeight;
                    headbobCameraPosition.z = ((Mathf.Sin(Mathf.PI * (2 * headbobCyclePosition))) * (ZTilt / 3)) * headbobWarmUp;
                }
                else
                {
                    headbobCameraPosition = Vector3.MoveTowards(headbobCameraPosition, Vector3.up * internalEyeHeight, Time.deltaTime / (headbobPower * 0.3f));
                    headbobWarmUp = 0.1f;
                }
                playerCamera.transform.localPosition = (Vector2)headbobCameraPosition;
                if (StepCycle > (headbobCyclePosition * 3)) { StepCycle = headbobCyclePosition + 0.5f; }
            }
        }*/
        void UpdateCameraPosition_3rdPerson()
        {

            //Camera Obstacle Check
            cameraObstCheck = new Ray(headPos + (quatHeadRot * (Vector3.forward * capsule.radius)), quatHeadRot * -Vector3.forward);
            if (Physics.SphereCast(cameraObstCheck, 0.5f, out cameraObstResult, maxCameraDistInternal, cameraObstructionIgnore, QueryTriggerInteraction.Ignore))
            {
                currentCameraZ = -(Vector3.Distance(headPos, cameraObstResult.point) * 0.9f);

            }
            else
            {
                currentCameraZ = Mathf.SmoothDamp(currentCameraZ, -(maxCameraDistInternal * 0.85f), ref cameraZRef, Time.deltaTime, 10, Time.fixedDeltaTime);
            }

            //Debugging
            if (enableMouseAndCameraDebugging)
            {
                Debug.Log(headRot);
                Debug.DrawRay(cameraObstCheck.origin, cameraObstCheck.direction * maxCameraDistance, Color.red);
                Debug.DrawRay(cameraObstCheck.origin, cameraObstCheck.direction * -currentCameraZ, Color.green);
            }
            currentCameraPos = headPos + (quatHeadRot * (Vector3.forward * currentCameraZ));
            playerCamera.transform.position = currentCameraPos;
            playerCamera.transform.rotation = quatHeadRot;
        }

        void UpdateBodyRotation_3rdPerson()
        {
            //if is moving, rotate capsule to match camera forward   //change button down to bool of isFiring or isTargeting
            if (!isIdle && !isSliding && currentGroundInfo.isGettingGroundInfo)
            {
                transform.rotation = (Quaternion.Euler(0, Mathf.MoveTowardsAngle(p_Rigidbody.rotation.eulerAngles.y, (Mathf.Atan2(InputDir.x, InputDir.z) * Mathf.Rad2Deg), 10), 0));
                //transform.rotation = Quaternion.Euler(0,Mathf.MoveTowardsAngle(transform.eulerAngles.y,(Mathf.Atan2(InputDir.x,InputDir.z)*Mathf.Rad2Deg),2.5f), 0);
            }
            else if (isSliding)
            {
                transform.localRotation = (Quaternion.Euler(Vector3.up * Mathf.MoveTowardsAngle(p_Rigidbody.rotation.eulerAngles.y, (Mathf.Atan2(p_Rigidbody.velocity.x, p_Rigidbody.velocity.z) * Mathf.Rad2Deg), 10)));
            }
            else if (!currentGroundInfo.isGettingGroundInfo && rotateCharacterToCameraForward)
            {
                transform.localRotation = (Quaternion.Euler(Vector3.up * Mathf.MoveTowardsAngle(p_Rigidbody.rotation.eulerAngles.y, headRot.y, 10)));
            }
        }
        #endregion

        #region Movement Functions
        void MovePlayer(Vector3 Direction, float Speed)
        {
            // GroundInfo gI = GetGroundInfo();
            isIdle = Direction.normalized.magnitude <= 0;
            _2DVelocity = Vector2.right * p_Rigidbody.velocity.x + Vector2.up * p_Rigidbody.velocity.z;
            speedToVelocityRatio = (Mathf.Lerp(0, 2, Mathf.InverseLerp(0, (sprintingSpeed / 50), _2DVelocity.magnitude)));
            _2DVelocityMag = Mathf.Clamp((walkingSpeed / 50) / _2DVelocity.magnitude, 0f, 2f);


            //Movement
            if ((currentGroundInfo.isGettingGroundInfo) && !Jumped && !isSliding && !atacando /*&& !doingPosInterp*/)
            {
                //Deceleration
                if (Direction.magnitude == 0 && p_Rigidbody.velocity.normalized.magnitude > 0.1f && !atacando)
                {
                    p_Rigidbody.AddForce(-new Vector3(p_Rigidbody.velocity.x, currentGroundInfo.isInContactWithGround ? p_Rigidbody.velocity.y - Physics.gravity.y : 0, p_Rigidbody.velocity.z) * (decelerationSpeed * Time.fixedDeltaTime), ForceMode.Force);
                }
                //normal speed
                else if ((currentGroundInfo.isGettingGroundInfo) && currentGroundInfo.groundAngle < hardSlopeLimit && currentGroundInfo.groundAngle_Raw < hardSlopeLimit && !atacando)
                {
                    p_Rigidbody.velocity = (Vector3.MoveTowards(p_Rigidbody.velocity, Vector3.ClampMagnitude(((Direction) * ((Speed) * Time.fixedDeltaTime)) + (Vector3.down), Speed / 50), 1));
                }
                capsule.sharedMaterial = InputDir.magnitude > 0 ? _ZeroFriction : _MaxFriction;
            }
            //Sliding
            else if (isSliding)
            {
                p_Rigidbody.AddForce(-(p_Rigidbody.velocity - Physics.gravity) * (slidingDeceleration * Time.fixedDeltaTime), ForceMode.Force);
            }

            //Air Control
            else if (!currentGroundInfo.isGettingGroundInfo)
            {
                p_Rigidbody.AddForce((((Direction * (walkingSpeed)) * Time.fixedDeltaTime) * airControlFactor * 5) * currentGroundInfo.groundAngleMultiplier_Inverse_persistent, ForceMode.Acceleration);
                p_Rigidbody.velocity = Vector3.ClampMagnitude((Vector3.right * p_Rigidbody.velocity.x + Vector3.forward * p_Rigidbody.velocity.z), (walkingSpeed / 50)) + (Vector3.up * p_Rigidbody.velocity.y);
                if (!currentGroundInfo.potentialStair && jumpEnhancements)
                {
                    if (p_Rigidbody.velocity.y < 0 && p_Rigidbody.velocity.y > Physics.gravity.y * 1.5f)
                    {
                        p_Rigidbody.velocity += Vector3.up * (Physics.gravity.y * (decentMultiplier) * Time.fixedDeltaTime);
                    }
                    else if (p_Rigidbody.velocity.y > 0 && !jumpInput_Momentary)
                    {
                        p_Rigidbody.velocity += Vector3.up * (Physics.gravity.y * (tapJumpMultiplier - 1) * Time.fixedDeltaTime);
                    }
                }
            }


        }
        void Jump(float Force)
        {
            if ((currentGroundInfo.isInContactWithGround) &&
                (currentGroundInfo.groundAngle < hardSlopeLimit) &&
                ((enableStaminaSystem && jumpingDepletesStamina) ? currentStaminaLevel > s_JumpStaminaDepletion * 1.2f : true) &&
                (Time.time > (jumpBlankingPeriod + 0.1f)) &&
                (currentStance == Stances.Standing && !Jumped))
            {

                Jumped = true;
                p_Rigidbody.velocity = (Vector3.right * p_Rigidbody.velocity.x) + (Vector3.forward * p_Rigidbody.velocity.z);
                p_Rigidbody.AddForce(Vector3.up * (Force / 10), ForceMode.Impulse);
                if (enableStaminaSystem && jumpingDepletesStamina)
                {
                    InstantStaminaReduction(s_JumpStaminaDepletion);
                }
                capsule.sharedMaterial = _ZeroFriction;
                jumpBlankingPeriod = Time.time;
            }
        }
        public void DoJump(float Force = 10.0f)
        {
            if (
                (Time.time > (jumpBlankingPeriod + 0.1f)) &&
                (currentStance == Stances.Standing))
            {
                Jumped = true;
                p_Rigidbody.velocity = (Vector3.right * p_Rigidbody.velocity.x) + (Vector3.forward * p_Rigidbody.velocity.z);
                p_Rigidbody.AddForce(Vector3.up * (Force / 10), ForceMode.Impulse);
                if (enableStaminaSystem && jumpingDepletesStamina)
                {
                    InstantStaminaReduction(s_JumpStaminaDepletion);
                }
                capsule.sharedMaterial = _ZeroFriction;
                jumpBlankingPeriod = Time.time;
            }
        }
     /*   void Slide()
        {
            if (!isSliding)
            {
                if (currentGroundInfo.isInContactWithGround)
                {
                    //do debug print
                    if (enableMovementDebugging) { print("Starting Slide."); }
                    p_Rigidbody.AddForce((transform.forward * ((sprintingSpeed)) + (Vector3.up * currentGroundInfo.groundInfluenceDirection.y)), ForceMode.Force);
                    cachedDirPreSlide = transform.forward;
                    cachedPosPreSlide = transform.position;
                    capsule.sharedMaterial = _ZeroFriction;
                    StartCoroutine(ApplyStance(slidingTransitionSpeed, Stances.Crouching));
                    isSliding = true;
                }
            }
            else if (slideInput_Momentary)
            {
                if (enableMovementDebugging) { print("Continuing Slide."); }
                if (Vector3.Distance(transform.position, cachedPosPreSlide) < maxFlatSlideDistance) { p_Rigidbody.AddForce(cachedDirPreSlide * (sprintingSpeed / 50), ForceMode.Force); }
                if (p_Rigidbody.velocity.magnitude > sprintingSpeed / 50) { p_Rigidbody.velocity = p_Rigidbody.velocity.normalized * (sprintingSpeed / 50); }
                else if (p_Rigidbody.velocity.magnitude < (crouchingSpeed / 25))
                {
                    if (enableMovementDebugging) { print("Slide too slow, ending slide into crouch."); }
                    //capsule.sharedMaterial = _MaxFrix;
                    isSliding = false;
                    isSprinting = false;
                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                    currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
                }
            }
            else
            {
                if (OverheadCheck())
                {
                    if (p_Rigidbody.velocity.magnitude > (walkingSpeed / 50))
                    {
                        if (enableMovementDebugging) { print("Key realeased, ending slide into a sprint."); }
                        isSliding = false;
                        StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                        currentGroundMovementSpeed = GroundSpeedProfiles.Sprinting;
                    }
                    else
                    {
                        if (enableMovementDebugging) { print("Key realeased, ending slide into a walk."); }
                        isSliding = false;
                        StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                        currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
                    }
                }
                else
                {
                    if (enableMovementDebugging) { print("Key realeased but there is an obstruction. Ending slide into crouch."); }
                    isSliding = false;
                    isSprinting = false;
                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                    currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
                }

            }
        } */
        void GetGroundInfo()
        {
            //to Get if we're actually touching ground.
            //to act as a normal and point buffer.
            currentGroundInfo.groundFromSweep = null;

            currentGroundInfo.groundFromSweep = Physics.SphereCastAll(transform.position, capsule.radius - 0.001f, Vector3.down, ((capsule.height / 2)) - (capsule.radius / 2), whatIsGround);
            currentGroundInfo.isInContactWithGround = Physics.Raycast(transform.position, Vector3.down, out currentGroundInfo.groundFromRay, (capsule.height / 2) + 0.25f, whatIsGround);

            if (Jumped && (Physics.Raycast(transform.position, Vector3.down, (capsule.height / 2) + 0.1f, whatIsGround) || Physics.CheckSphere(transform.position - (Vector3.up * ((capsule.height / 2) - (capsule.radius - 0.05f))), capsule.radius, whatIsGround)) && Time.time > (jumpBlankingPeriod + 0.1f))
            {
                Jumped = false;
            }

            //if(Result.isGrounded){
            if (currentGroundInfo.groundFromSweep != null && currentGroundInfo.groundFromSweep.Length != 0)
            {
                currentGroundInfo.isGettingGroundInfo = true;
                currentGroundInfo.groundNormals_lowgrade.Clear();
                currentGroundInfo.groundNormals_highgrade.Clear();
                foreach (RaycastHit hit in currentGroundInfo.groundFromSweep)
                {
                    if (hit.point.y > currentGroundInfo.groundFromRay.point.y && Vector3.Angle(hit.normal, Vector3.up) < hardSlopeLimit)
                    {
                        currentGroundInfo.groundNormals_lowgrade.Add(hit.normal);
                    }
                    else
                    {
                        currentGroundInfo.groundNormals_highgrade.Add(hit.normal);
                    }
                }
                if (currentGroundInfo.groundNormals_lowgrade.Any())
                {
                    currentGroundInfo.groundNormal_Averaged = Average(currentGroundInfo.groundNormals_lowgrade);
                }
                else
                {
                    currentGroundInfo.groundNormal_Averaged = Average(currentGroundInfo.groundNormals_highgrade);
                }
                currentGroundInfo.groundNormal_Raw = currentGroundInfo.groundFromRay.normal;
                currentGroundInfo.groundRawYPosition = currentGroundInfo.groundFromSweep.Average(x => (x.point.y > currentGroundInfo.groundFromRay.point.y && Vector3.Angle(x.normal, Vector3.up) < hardSlopeLimit) ? x.point.y : currentGroundInfo.groundFromRay.point.y); //Mathf.MoveTowards(currentGroundInfo.groundRawYPosition, currentGroundInfo.groundFromSweep.Average(x=> (x.point.y > currentGroundInfo.groundFromRay.point.y && Vector3.Dot(x.normal,Vector3.up)<-0.25f) ? x.point.y :  currentGroundInfo.groundFromRay.point.y),Time.deltaTime*2);

            }
            else
            {
                currentGroundInfo.isGettingGroundInfo = false;
                currentGroundInfo.groundNormal_Averaged = currentGroundInfo.groundFromRay.normal;
                currentGroundInfo.groundNormal_Raw = currentGroundInfo.groundFromRay.normal;
                currentGroundInfo.groundRawYPosition = currentGroundInfo.groundFromRay.point.y;
            }

            if (currentGroundInfo.isGettingGroundInfo) { currentGroundInfo.groundAngleMultiplier_Inverse_persistent = currentGroundInfo.groundAngleMultiplier_Inverse; }
            //{
            currentGroundInfo.groundInfluenceDirection = Vector3.MoveTowards(currentGroundInfo.groundInfluenceDirection, Vector3.Cross(currentGroundInfo.groundNormal_Averaged, Vector3.Cross(currentGroundInfo.groundNormal_Averaged, Vector3.up)).normalized, 2 * Time.fixedDeltaTime);
            currentGroundInfo.groundInfluenceDirection.y = 0;
            currentGroundInfo.groundAngle = Vector3.Angle(currentGroundInfo.groundNormal_Averaged, Vector3.up);
            currentGroundInfo.groundAngle_Raw = Vector3.Angle(currentGroundInfo.groundNormal_Raw, Vector3.up);
            currentGroundInfo.groundAngleMultiplier_Inverse = ((currentGroundInfo.groundAngle - 90) * -1) / 90;
            currentGroundInfo.groundAngleMultiplier = ((currentGroundInfo.groundAngle)) / 90;
            //
            currentGroundInfo.groundTag = currentGroundInfo.isInContactWithGround ? currentGroundInfo.groundFromRay.transform.tag : string.Empty;
            if (Physics.Raycast(transform.position + (Vector3.down * ((capsule.height * 0.5f) - 0.1f)), InputDir, out currentGroundInfo.stairCheck_RiserCheck, capsule.radius + 0.1f, whatIsGround))
            {
                if (Physics.Raycast(currentGroundInfo.stairCheck_RiserCheck.point + (currentGroundInfo.stairCheck_RiserCheck.normal * -0.05f) + Vector3.up, Vector3.down, out currentGroundInfo.stairCheck_HeightCheck, 1.1f))
                {
                    if (!Physics.Raycast(transform.position + (Vector3.down * ((capsule.height * 0.5f) - maxStairRise)) + InputDir * (capsule.radius - 0.05f), InputDir, 0.2f, whatIsGround))
                    {
                        if (!isIdle && currentGroundInfo.stairCheck_HeightCheck.point.y > (currentGroundInfo.stairCheck_RiserCheck.point.y + 0.025f) /* Vector3.Angle(currentGroundInfo.groundFromRay.normal, Vector3.up)<5 */ && Vector3.Angle(currentGroundInfo.groundNormal_Averaged, currentGroundInfo.stairCheck_RiserCheck.normal) > 0.5f)
                        {
                            p_Rigidbody.position -= Vector3.up * -0.1f;
                            currentGroundInfo.potentialStair = true;
                        }
                    }
                    else { currentGroundInfo.potentialStair = false; }
                }
            }
            else { currentGroundInfo.potentialStair = false; }


            currentGroundInfo.playerGroundPosition = Mathf.MoveTowards(currentGroundInfo.playerGroundPosition, currentGroundInfo.groundRawYPosition + (capsule.height / 2) + 0.01f, 0.05f);
            //}

            if (currentGroundInfo.isInContactWithGround && enableFootstepSounds && shouldCalculateFootstepTriggers)
            {
                if (currentGroundInfo.groundFromRay.collider is TerrainCollider)
                {
                    currentGroundInfo.groundMaterial = null;
                    currentGroundInfo.groundPhysicMaterial = currentGroundInfo.groundFromRay.collider.sharedMaterial;
                    currentGroundInfo.currentTerrain = currentGroundInfo.groundFromRay.transform.GetComponent<Terrain>();
                    if (currentGroundInfo.currentTerrain)
                    {
                        Vector2 XZ = (Vector2.right * (((transform.position.x - currentGroundInfo.currentTerrain.transform.position.x) / currentGroundInfo.currentTerrain.terrainData.size.x)) * currentGroundInfo.currentTerrain.terrainData.alphamapWidth) + (Vector2.up * (((transform.position.z - currentGroundInfo.currentTerrain.transform.position.z) / currentGroundInfo.currentTerrain.terrainData.size.z)) * currentGroundInfo.currentTerrain.terrainData.alphamapHeight);
                        float[,,] aMap = currentGroundInfo.currentTerrain.terrainData.GetAlphamaps((int)XZ.x, (int)XZ.y, 1, 1);
                        for (int i = 0; i < aMap.Length; i++)
                        {
                            if (aMap[0, 0, i] == 1)
                            {
                                currentGroundInfo.groundLayer = currentGroundInfo.currentTerrain.terrainData.terrainLayers[i];
                                break;
                            }
                        }
                    }
                    else { currentGroundInfo.groundLayer = null; }
                }
                else
                {
                    currentGroundInfo.groundLayer = null;
                    currentGroundInfo.groundPhysicMaterial = currentGroundInfo.groundFromRay.collider.sharedMaterial;
                    currentGroundInfo.currentMesh = currentGroundInfo.groundFromRay.transform.GetComponent<MeshFilter>().sharedMesh;
                    if (currentGroundInfo.currentMesh && currentGroundInfo.currentMesh.isReadable)
                    {
                        int limit = currentGroundInfo.groundFromRay.triangleIndex * 3, submesh;
                        for (submesh = 0; submesh < currentGroundInfo.currentMesh.subMeshCount; submesh++)
                        {
                            int indices = currentGroundInfo.currentMesh.GetTriangles(submesh).Length;
                            if (indices > limit) { break; }
                            limit -= indices;
                        }
                        currentGroundInfo.groundMaterial = currentGroundInfo.groundFromRay.transform.GetComponent<Renderer>().sharedMaterials[submesh];
                    }
                    else { currentGroundInfo.groundMaterial = currentGroundInfo.groundFromRay.collider.GetComponent<MeshRenderer>().sharedMaterial; }
                }
            }
            else { currentGroundInfo.groundMaterial = null; currentGroundInfo.groundLayer = null; currentGroundInfo.groundPhysicMaterial = null; }
#if UNITY_EDITOR
            if (enableGroundingDebugging)
            {
                print("Grounded: " + currentGroundInfo.isInContactWithGround + ", Ground Hits: " + currentGroundInfo.groundFromSweep.Length + ", Ground Angle: " + currentGroundInfo.groundAngle.ToString("0.00") + ", Ground Multi: " + currentGroundInfo.groundAngleMultiplier.ToString("0.00") + ", Ground Multi Inverse: " + currentGroundInfo.groundAngleMultiplier_Inverse.ToString("0.00"));
                print("Ground mesh readable for dynamic foot steps: " + currentGroundInfo.currentMesh?.isReadable);
                Debug.DrawRay(transform.position, Vector3.down * ((capsule.height / 2) + 0.1f), Color.green);
                Debug.DrawRay(transform.position, currentGroundInfo.groundInfluenceDirection, Color.magenta);
                Debug.DrawRay(transform.position + (Vector3.down * ((capsule.height * 0.5f) - 0.05f)) + InputDir * (capsule.radius - 0.05f), InputDir * (capsule.radius + 0.1f), Color.cyan);
                Debug.DrawRay(transform.position + (Vector3.down * ((capsule.height * 0.5f) - 0.5f)) + InputDir * (capsule.radius - 0.05f), InputDir * (capsule.radius + 0.3f), new Color(0, .2f, 1, 1));
            }
#endif
        }
        void GroundMovementSpeedUpdate()
        {
#if SAIO_ENABLE_PARKOUR
        if(!isVaulting)
#endif
            {
                switch (currentGroundMovementSpeed)
                {
                    case GroundSpeedProfiles.Walking:
                        {
                            if (isCrouching || isSprinting)
                            {
                                isSprinting = false;
                                isCrouching = false;
                                currentGroundSpeed = walkingSpeed;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                            }
#if SAIO_ENABLE_PARKOUR
                    if(vaultInput && canVault){VaultCheck();}
#endif
                            //check for state change call
                            if ((canCrouch && crouchInput_FrameOf) || crouchOverride)
                            {
                                isCrouching = true;
                                isSprinting = false;
                                currentGroundSpeed = crouchingSpeed;
                                currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                                break;
                            }
                            else if ((canSprint && sprintInput_FrameOf && ((enableStaminaSystem && jumpingDepletesStamina) ? currentStaminaLevel > s_minimumStaminaToSprint : true)) /* && (enableSurvivalStats ? (!currentSurvivalStats.isDehydrated && !currentSurvivalStats.isStarving) : true)) */ || sprintOverride)
                            {
                                isCrouching = false;
                                isSprinting = true;
                                currentGroundSpeed = sprintingSpeed;
                                currentGroundMovementSpeed = GroundSpeedProfiles.Sprinting;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                            }
                            break;
                        }

                    case GroundSpeedProfiles.Crouching:
                        {
                            if (!isCrouching)
                            {
                                isCrouching = true;
                                isSprinting = false;
                                currentGroundSpeed = crouchingSpeed;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                            }


                            //check for state change call
                            if ((toggleCrouch ? crouchInput_FrameOf : !crouchInput_Momentary) && !crouchOverride && OverheadCheck())
                            {
                                isCrouching = false;
                                isSprinting = false;
                                currentGroundSpeed = walkingSpeed;
                                currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                                break;
                            }
                            else if (((canSprint && sprintInput_FrameOf && ((enableStaminaSystem && jumpingDepletesStamina) ? currentStaminaLevel > s_minimumStaminaToSprint : true) /*&& (enableSurvivalStats ? (!currentSurvivalStats.isDehydrated && !currentSurvivalStats.isStarving) : true)*/) || sprintOverride) && OverheadCheck())
                            {
                                isCrouching = false;
                                isSprinting = true;
                                currentGroundSpeed = sprintingSpeed;
                                currentGroundMovementSpeed = GroundSpeedProfiles.Sprinting;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                            }
                            break;
                        }

                    case GroundSpeedProfiles.Sprinting:
                        {
                            //if(!isIdle)
                            {
                                if (!isSprinting)
                                {
                                    isCrouching = false;
                                    isSprinting = true;
                                    currentGroundSpeed = sprintingSpeed;
                                    StopCoroutine("ApplyStance");
                                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                                }
#if SAIO_ENABLE_PARKOUR
                        if((vaultInput || autoVaultWhenSpringing) && canVault){VaultCheck();}
#endif
                                //check for state change call
                                if (canSlide && !isIdle && slideInput_FrameOf && currentGroundInfo.isInContactWithGround)
                                {
                                    //Slide();
                                    currentGroundMovementSpeed = GroundSpeedProfiles.Sliding;
                                    break;
                                }


                                else if ((canCrouch && crouchInput_FrameOf) || crouchOverride)
                                {
                                    isCrouching = true;
                                    isSprinting = false;
                                    currentGroundSpeed = crouchingSpeed;
                                    currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
                                    StopCoroutine("ApplyStance");
                                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                                    break;
                                    //Can't leave sprint in toggle sprint.
                                }
                                else if ((toggleSprint ? sprintInput_FrameOf : !sprintInput_Momentary) && !sprintOverride)
                                {
                                    isCrouching = false;
                                    isSprinting = false;
                                    currentGroundSpeed = walkingSpeed;
                                    currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
                                    StopCoroutine("ApplyStance");
                                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                                }
                                break;
                            }
                        }
                    case GroundSpeedProfiles.Sliding:
                        {
                        }
                        break;
                }
            }
        }
        IEnumerator ApplyStance(float smoothSpeed, Stances newStance)
        {
            currentStance = newStance;
            float targetCapsuleHeight = currentStance == Stances.Standing ? standingHeight : crouchingHeight;
            float targetEyeHeight = currentStance == Stances.Standing ? standingEyeHeight : crouchingEyeHeight;
            while (!Mathf.Approximately(capsule.height, targetCapsuleHeight))
            {
                changingStances = true;
                capsule.height = (smoothSpeed > 0 ? Mathf.MoveTowards(capsule.height, targetCapsuleHeight, stanceTransitionSpeed * Time.fixedDeltaTime) : targetCapsuleHeight);
                internalEyeHeight = (smoothSpeed > 0 ? Mathf.MoveTowards(internalEyeHeight, targetEyeHeight, stanceTransitionSpeed * Time.fixedDeltaTime) : targetCapsuleHeight);

                if (currentStance == Stances.Crouching && currentGroundInfo.isGettingGroundInfo)
                {
                    p_Rigidbody.velocity = p_Rigidbody.velocity + (Vector3.down * 2);
                    if (enableMovementDebugging) { print("Applying Stance and applying down force "); }
                }
                yield return new WaitForFixedUpdate();
            }
            changingStances = false;
            yield return null;
        }
        bool OverheadCheck()
        {    //Returns true when there is no obstruction.
            bool result = false;
            if (Physics.Raycast(transform.position, Vector3.up, standingHeight - (capsule.height / 2), whatIsGround)) { result = true; }
            return !result;
        }
        Vector3 Average(List<Vector3> vectors)
        {
            Vector3 returnVal = default(Vector3);
            vectors.ForEach(x => { returnVal += x; });
            returnVal /= vectors.Count();
            return returnVal;
        }

        #endregion

        #region Stamina System
        private void CalculateStamina()
        {
            if (isSprinting && !ignoreStamina && !isIdle)
            {
                if (currentStaminaLevel != 0)
                {
                    currentStaminaLevel = Mathf.MoveTowards(currentStaminaLevel, 0, s_depletionSpeed * Time.deltaTime);
                }
                else if (!isSliding) { currentGroundMovementSpeed = GroundSpeedProfiles.Walking; }
                staminaIsChanging = true;
            }
            else if (currentStaminaLevel != Stamina && !ignoreStamina /*&& (enableSurvivalStats ? (!currentSurvivalStats.isDehydrated && !currentSurvivalStats.isStarving) : true)*/)
            {
                currentStaminaLevel = Mathf.MoveTowards(currentStaminaLevel, Stamina, s_regenerationSpeed * Time.deltaTime);
                staminaIsChanging = true;
            }
            else
            {
                staminaIsChanging = false;
            }
        }
        public void InstantStaminaReduction(float Reduction)
        {
            if (!ignoreStamina && enableStaminaSystem) { currentStaminaLevel = Mathf.Clamp(currentStaminaLevel -= Reduction, 0, Stamina); }
        }
        #endregion

        #region Footstep System
        void CalculateFootstepTriggers()
        {
            if (enableFootstepSounds && footstepTriggeringMode == FootstepTriggeringMode.calculatedTiming && shouldCalculateFootstepTriggers)
            {
                if (_2DVelocity.magnitude > (currentGroundSpeed / 100) && !isIdle)
                {
                    if (cameraPerspective == PerspectiveModes._1stPerson)
                    {
                        /*if ((enableHeadbob ? headbobCyclePosition : Time.time) > StepCycle && currentGroundInfo.isGettingGroundInfo && !isSliding)
                        {
                            //print("Steped");
                            CallFootstepClip();
                            StepCycle = enableHeadbob ? (headbobCyclePosition + 0.5f) : (Time.time + ((stepTiming * _2DVelocityMag) * 2));
                        }*/
                    }
                    else
                    {
                        if (Time.time > StepCycle && currentGroundInfo.isGettingGroundInfo && !isSliding)
                        {
                            //print("Steped");
                            CallFootstepClip();
                            //StepCycle = (Time.time+((stepTiming*_2DVelocityMag)*2));


                            if (Input.GetKey(KeyCode.LeftShift))
                            {
                                StepCycle = (Time.time + ((modificadorCorriendo * _2DVelocityMag) * 2));
                            }
                            else
                            {
                                StepCycle = (Time.time + ((stepTiming * _2DVelocityMag) * 2));
                            }
                        }
                    }
                }
            }
        }
        public void CallFootstepClip()
        {
            if (playerAudioSource)
            {
                if (enableFootstepSounds && footstepSoundSet.Any())
                {
                    for (int i = 0; i < footstepSoundSet.Count(); i++)
                    {

                        if (footstepSoundSet[i].profileTriggerType == MatProfileType.Material)
                        {
                            if (footstepSoundSet[i]._Materials.Contains(currentGroundInfo.groundMaterial))
                            {
                                currentClipSet = footstepSoundSet[i].footstepClips;
                                break;
                            }
                            else if (i == footstepSoundSet.Count - 1)
                            {
                                currentClipSet = null;
                            }
                        }

                        else if (footstepSoundSet[i].profileTriggerType == MatProfileType.physicMaterial)
                        {
                            if (footstepSoundSet[i]._physicMaterials.Contains(currentGroundInfo.groundPhysicMaterial))
                            {
                                currentClipSet = footstepSoundSet[i].footstepClips;
                                break;
                            }
                            else if (i == footstepSoundSet.Count - 1)
                            {
                                currentClipSet = null;
                            }
                        }

                        else if (footstepSoundSet[i].profileTriggerType == MatProfileType.terrainLayer)
                        {
                            if (footstepSoundSet[i]._Layers.Contains(currentGroundInfo.groundLayer))
                            {
                                currentClipSet = footstepSoundSet[i].footstepClips;
                                break;
                            }
                            else if (i == footstepSoundSet.Count - 1)
                            {
                                currentClipSet = null;
                            }
                        }
                    }

                    if (currentClipSet != null && currentClipSet.Any())
                    {
                        playerAudioSource.PlayOneShot(currentClipSet[Random.Range(0, currentClipSet.Count())]);
                    }
                }
            }
        }
        #endregion


        #region Animator Update
        void UpdateAnimationTriggers(bool zeroOut = false)
        {
            switch (cameraPerspective)
            {
                case PerspectiveModes._1stPerson:
                    {
                        /*if (_1stPersonCharacterAnimator)
                        {
                            //Setup Fistperson animation triggers here.

                        }*/
                    }
                    break;

                case PerspectiveModes._3rdPerson:
                    {
                        if (_3rdPersonCharacterAnimator)
                        {
                            if (stickRendererToCapsuleBottom)
                            {
                                _3rdPersonCharacterAnimator.transform.position = (Vector3.right * _3rdPersonCharacterAnimator.transform.position.x) + (Vector3.up * (transform.position.y - (capsule.height / 2))) + (Vector3.forward * _3rdPersonCharacterAnimator.transform.position.z);
                            }
                            if (!zeroOut)
                            {
                                //Setup Thirdperson animation triggers here.
                                if (a_velocity != "")
                                {
                                    _3rdPersonCharacterAnimator.SetFloat(a_velocity, p_Rigidbody.velocity.sqrMagnitude);
                                }
                                if (a_2DVelocity != "")
                                {
                                    _3rdPersonCharacterAnimator.SetFloat(a_2DVelocity, _2DVelocity.magnitude);
                                }
                                if (a_Idle != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Idle, isIdle);
                                }
                                if (a_Sprinting != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Sprinting, isSprinting);
                                }
                                if (a_Crouching != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Crouching, isCrouching);
                                }
                                if (a_Sliding != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Sliding, isSliding);
                                }
                                if (a_Jumped != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Jumped, Jumped);
                                }
                                if (a_Grounded != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Grounded, currentGroundInfo.isInContactWithGround);
                                }
                                if(a_facon != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_facon, tengoFacon);
                                }
                                if(a_faconazo != "" && Input.GetKey(KeyCode.Mouse0) && !Jumped && !atacando && tengoFacon && !cubriendose && !controller.enMenuRadial)
                                {
                                    atacando = true;
                                    p_Rigidbody.velocity = Vector3.zero;
                                    _3rdPersonCharacterAnimator.SetTrigger(a_faconazo);
                                    InstantStaminaReduction(s_FacaStaminaDepletion);
                                }
                                if (a_esquivar != "" && Input.GetKey(KeyCode.Space) && !Jumped && !atacando && tengoFacon && !cubriendose)
                                {
                                    //Vector3 ultPos = transform.position;
                                    //ultimaPosicion = ultPos;
                                    atacando = true;
                                    _3rdPersonCharacterAnimator.SetTrigger(a_esquivar);
                                    p_Rigidbody.velocity = Vector3.zero;
                                    InstantStaminaReduction(s_FacaStaminaDepletion);
                                }
                                if (a_poncho != "" && Input.GetKeyDown(KeyCode.Mouse1) && !Jumped && !atacando && tengoFacon && !cubriendose)
                                {
                                    cubriendose = true;
                                    _3rdPersonCharacterAnimator.SetBool(a_poncho, true);
                                    p_Rigidbody.velocity = Vector3.zero;
                                }
                                if (Input.GetKeyUp(KeyCode.Mouse1) && cubriendose)
                                {
                                    cubriendose = false;
                                    _3rdPersonCharacterAnimator.SetBool(a_poncho, false);
                                }
                                if (a_velXZ != "")
                                {
                                    _3rdPersonCharacterAnimator.SetFloat(a_velXZ, velXZ.magnitude);
                                }
                                if(a_rifle != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_rifle, tengoRifle);
                                }
                                if (a_boleadoras != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_boleadoras, tengoBoleadoras);
                                }
                                if (a_VelX != "" && tengoBoleadoras)
                                {
                                    _3rdPersonCharacterAnimator.SetFloat(a_VelX, horizontal);
                                }
                                if (a_VelY != "" && tengoBoleadoras)
                                {
                                    _3rdPersonCharacterAnimator.SetFloat(a_VelY, vertical);
                                }
                                if (a_lanzar != "" && tengoBoleadoras && Input.GetKey(KeyCode.Mouse0) && !controller.enMenuRadial)
                                {
                                    _3rdPersonCharacterAnimator.SetTrigger(a_lanzar);
                                }
                                if(a_isDeath != "" && estaMuerto && !murio)
                                {
                                    murio = true;
                                    _3rdPersonCharacterAnimator.SetTrigger(a_isDeath);
                                }

                            }
                            else
                            {
                                if (a_velocity != "")
                                {
                                    _3rdPersonCharacterAnimator.SetFloat(a_velocity, 0);
                                }
                                if (a_2DVelocity != "")
                                {
                                    _3rdPersonCharacterAnimator.SetFloat(a_2DVelocity, 0);
                                }
                                if (a_Idle != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Idle, true);
                                }
                                if (a_Sprinting != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Sprinting, false);
                                }
                                if (a_Crouching != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Crouching, false);
                                }
                                if (a_Sliding != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Sliding, false);
                                }
                                if (a_Jumped != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Jumped, false);
                                }
                                if (a_Grounded != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_Grounded, true);
                                }
                                if(a_facon != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_facon, false);
                                }
                                if (a_faconazo != "" && Input.GetKey(KeyCode.Mouse0) && !Jumped && !atacando && tengoFacon)
                                {
                                    p_Rigidbody.velocity = Vector3.zero;
                                    _3rdPersonCharacterAnimator.SetTrigger(a_faconazo);
                                    atacando = true;
                                    InstantStaminaReduction(s_FacaStaminaDepletion);
                                }
                                if (a_esquivar != "" && Input.GetKey(KeyCode.Space) && !Jumped && !atacando && tengoFacon)
                                {
                                    atacando = true;
                                    _3rdPersonCharacterAnimator.SetTrigger(a_esquivar);
                                    p_Rigidbody.velocity = Vector3.zero;
                                    InstantStaminaReduction(s_FacaStaminaDepletion);
                                }
                                if (a_poncho != "" && Input.GetKeyDown(KeyCode.Mouse1) && !Jumped && !atacando && tengoFacon && !cubriendose)
                                {
                                    cubriendose = true;
                                    _3rdPersonCharacterAnimator.SetBool(a_poncho, true);
                                    p_Rigidbody.velocity = Vector3.zero;
                                }
                                if (Input.GetKeyUp(KeyCode.Mouse1) && cubriendose)
                                {
                                    cubriendose = false;
                                    _3rdPersonCharacterAnimator.SetBool(a_poncho, false);
                                }
                                if (a_velXZ != "")
                                {
                                    _3rdPersonCharacterAnimator.SetFloat(a_velXZ, 0);
                                }
                                if (a_rifle != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_rifle, false);
                                }
                                if (a_boleadoras != "")
                                {
                                    _3rdPersonCharacterAnimator.SetBool(a_boleadoras, false);
                                }
                                if (a_VelX != "" && tengoBoleadoras)
                                {
                                    _3rdPersonCharacterAnimator.SetFloat(a_VelX, 0);
                                }
                                if (a_VelY != "" && tengoBoleadoras)
                                {
                                    _3rdPersonCharacterAnimator.SetFloat(a_VelY, 0);
                                }
                                if (a_lanzar != "" && tengoBoleadoras && Input.GetKey(KeyCode.Mouse0) && !controller.enMenuRadial)
                                {
                                    _3rdPersonCharacterAnimator.SetTrigger(a_lanzar);
                                }
                                if (a_isDeath != "" && estaMuerto && !murio)
                                {
                                    _3rdPersonCharacterAnimator.SetTrigger(a_isDeath);
                                }
                            }

                        }

                    }
                    break;
            }
        }
        #endregion

      /*  public void PausePlayer(PauseModes pauseMode)
        {
            controllerPaused = true;
            switch (pauseMode)
            {
                case PauseModes.MakeKinematic:
                    {
                        p_Rigidbody.isKinematic = true;
                    }
                    break;

                case PauseModes.FreezeInPlace:
                    {
                        p_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                    }
                    break;

                case PauseModes.BlockInputOnly:
                    {

                    }
                    break;
            }

            p_Rigidbody.velocity = Vector3.zero;
            InputDir = Vector2.zero;
            MovInput = Vector2.zero;
            MovInput_Smoothed = Vector2.zero;
            capsule.sharedMaterial = _MaxFriction;

            UpdateAnimationTriggers(true);
            if (a_velocity != "")
            {
                _3rdPersonCharacterAnimator.SetFloat(a_velocity, 0);
            }
        }
        public void UnpausePlayer(float delay = 0)
        {
            if (delay == 0)
            {
                controllerPaused = false;
                p_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                p_Rigidbody.isKinematic = false;
            }
            else
            {
                StartCoroutine(UnpausePlayerI(delay));
            }
        }
        IEnumerator UnpausePlayerI(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            controllerPaused = false;
            p_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            p_Rigidbody.isKinematic = false;
        }*/

    }


    #region Classes and Enums
    [System.Serializable]
    public class GroundInfo
    {
        public bool isInContactWithGround, isGettingGroundInfo, potentialStair;
        public float groundAngleMultiplier_Inverse = 1, groundAngleMultiplier_Inverse_persistent = 1, groundAngleMultiplier = 0, groundAngle, groundAngle_Raw, playerGroundPosition, groundRawYPosition;
        public Vector3 groundInfluenceDirection, groundNormal_Averaged, groundNormal_Raw;
        public List<Vector3> groundNormals_lowgrade = new List<Vector3>(), groundNormals_highgrade;
        public string groundTag;
        public Material groundMaterial;
        public TerrainLayer groundLayer;
        public PhysicMaterial groundPhysicMaterial;
        internal Terrain currentTerrain;
        internal Mesh currentMesh;
        internal RaycastHit groundFromRay, stairCheck_RiserCheck, stairCheck_HeightCheck;
        internal RaycastHit[] groundFromSweep;


    }
    [System.Serializable]
    public class GroundMaterialProfile
    {
        public MatProfileType profileTriggerType = MatProfileType.Material;
        public List<Material> _Materials;
        public List<PhysicMaterial> _physicMaterials;
        public List<TerrainLayer> _Layers;
        public List<AudioClip> footstepClips = new List<AudioClip>();
    }
    [System.Serializable]
    public class SurvivalStats
    {
        public float Health = 250.0f, Hunger = 100.0f, Hydration = 100f;
        public bool hasLowHealth, isStarving, isDehydrated;
    }
    public enum StatSelector { Health, Hunger, Hydration }
    public enum MatProfileType { Material, terrainLayer, physicMaterial }
    public enum FootstepTriggeringMode { calculatedTiming, calledFromAnimations }
    public enum PerspectiveModes { _1stPerson, _3rdPerson }
    public enum ViewInputModes { Traditional, Retro }
    public enum MouseInputInversionModes { None, X, Y, Both }
    public enum GroundSpeedProfiles { Crouching, Walking, Sprinting, Sliding }
    public enum Stances { Standing, Crouching }
    public enum PauseModes { MakeKinematic, FreezeInPlace, BlockInputOnly }
    #endregion


    #region Editor Scripting
#if UNITY_EDITOR
    [CustomEditor(typeof(JuanMoveBehaviour))]
    public class SuperFPEditor : Editor
    {
        Color32 statBackingColor = new Color32(64, 64, 64, 255);

        GUIStyle labelHeaderStyle;
        GUIStyle l_scriptHeaderStyle;
        GUIStyle labelSubHeaderStyle;
        GUIStyle clipSetLabelStyle;
        GUIStyle SupportButtonStyle;
        GUIStyle ShowMoreStyle;
        GUIStyle BoxPanel;
        Texture2D BoxPanelColor;
        JuanMoveBehaviour t;
        SerializedObject tSO, SurvivalStatsTSO;
        SerializedProperty interactableLayer, obstructionMaskField, groundLayerMask, groundMatProf, defaultSurvivalStats, currentSurvivalStats;
        static bool cameraSettingsFoldout = false, movementSettingFoldout = false, survivalStatsFoldout, footStepFoldout = false;

        public void OnEnable()
        {
            t = (JuanMoveBehaviour)target;
            tSO = new SerializedObject(t);
            SurvivalStatsTSO = new SerializedObject(t);
            obstructionMaskField = tSO.FindProperty("cameraObstructionIgnore");
            groundLayerMask = tSO.FindProperty("whatIsGround");
            groundMatProf = tSO.FindProperty("footstepSoundSet");
            interactableLayer = tSO.FindProperty("interactableLayer");
            BoxPanelColor = new Texture2D(1, 1, TextureFormat.RGBAFloat, false); ;
            BoxPanelColor.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.2f));
            BoxPanelColor.Apply();
        }

        public override void OnInspectorGUI()
        {

            #region Style Null Check
            labelHeaderStyle = labelHeaderStyle != null ? labelHeaderStyle : new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 };
            l_scriptHeaderStyle = l_scriptHeaderStyle != null ? l_scriptHeaderStyle : new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true, fontSize = 16 };
            labelSubHeaderStyle = labelSubHeaderStyle != null ? labelSubHeaderStyle : new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 10, richText = true };
            ShowMoreStyle = ShowMoreStyle != null ? ShowMoreStyle : new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, margin = new RectOffset(15, 0, 0, 0), fontStyle = FontStyle.Bold, fontSize = 11, richText = true };
            clipSetLabelStyle = labelSubHeaderStyle != null ? labelSubHeaderStyle : new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };
            SupportButtonStyle = SupportButtonStyle != null ? SupportButtonStyle : new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 10, richText = true };
            BoxPanel = BoxPanel != null ? BoxPanel : new GUIStyle(GUI.skin.box) { normal = { background = BoxPanelColor } };
            #endregion

            /*  #region PlaymodeWarning
              if (Application.isPlaying)
              {
                  EditorGUILayout.HelpBox("It is recommended you switch to another Gameobject's inspector, Updates to this inspector panel during playmode can cause lag in the rigidbody calculations and cause unwanted adverse effects to gameplay. \n\n Please note this is NOT an issue in application builds.", MessageType.Warning);
              }
              #endregion*/

            /*#region Label  
            EditorGUILayout.Space();
            //Label A
            //GUILayout.Label("<b><i><size=16><color=#B2F9CF>S</color><color=#F9B2DC>U</color><color=#CFB2F9>P</color><color=#B2F9F3>E</color><color=#F9CFB2>R</color></size></i><size=12>Character Controller</size></b>",l_scriptHeaderStyle,GUILayout.ExpandWidth(true));

            //Label B
            //GUILayout.Label("<b><i><size=16><color=#3FB8AF>S</color><color=#7FC7AF>U</color><color=#DAD8A7>P</color><color=#FF9E9D>E</color><color=#FF3D7F>R</color></size></i><size=12>Character Controller</size></b>",l_scriptHeaderStyle,GUILayout.ExpandWidth(true));

            //Label C 
      
            //GUILayout.Label("<b><i><size=18><color=#FC80A5>S</color><color=#FFFF9F>U</color><color=#99FF99>P</color><color=#76D7EA>E</color><color=#BF8FCC>R</color></size></i></b> <size=12><i>Character Controller</i></size>", l_scriptHeaderStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); EditorGUILayout.Space();
            #endregion*/
            t.controller = (Controller)EditorGUILayout.ObjectField(new GUIContent("Player", "The "), t.controller, typeof(Controller), true);
            t.posCamera = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Posicion Camara", "The "), t.posCamera, typeof(GameObject), true);
            t.speed = EditorGUILayout.Slider(new GUIContent("", ""), t.speed, 0.0f, 10.0f);
            t.speedRotate = EditorGUILayout.Slider(new GUIContent("", ""), t.speedRotate, 50.0f, 300.0f);

            #region Camera Settings
            GUILayout.Label("Camera Settings", labelHeaderStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(BoxPanel);
            t.enableCameraControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Camera Control", "Should the player have control over the camera?"), t.enableCameraControl);
            t.playerCamera = (Camera)EditorGUILayout.ObjectField(new GUIContent("Player Camera", "The Camera Attached to the Player."), t.playerCamera, typeof(Camera), true);
            //t.cameraPerspective = (PerspectiveModes)EditorGUILayout.EnumPopup(new GUIContent("Camera Perspective Mode", "The current perspective of the character."), t.cameraPerspective);
            //if(t.cameraPerspective == PerspectiveModes._3rdPerson){EditorGUILayout.HelpBox("3rd Person perspective is currently very experimental. Bugs and other adverse effects may occur.",MessageType.Info);}

            //EditorGUI.indentLevel--;

            if (cameraSettingsFoldout)
            {
                t.automaticallySwitchPerspective = EditorGUILayout.ToggleLeft(new GUIContent("Automatically Switch Perspective", "Should the Camera perspective mode automatically change based on the distance between the camera and the character's head?"), t.automaticallySwitchPerspective);
/*#if ENABLE_INPUT_SYSTEM
            t.perspectiveSwitchingKey = (Key)EditorGUILayout.EnumPopup(new GUIContent("Perspective Switch Key", "The keyboard key used to switch perspective modes. Set to none if you do not wish to allow perspective switching"),t.perspectiveSwitchingKey);
#else
                if (!t.automaticallySwitchPerspective) { t.perspectiveSwitchingKey_L = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Perspective Switch Key", "The keyboard key used to switch perspective modes. Set to none if you do not wish to allow perspective switching"), t.perspectiveSwitchingKey_L); }
#endif*/
                t.mouseInputInversion = (MouseInputInversionModes)EditorGUILayout.EnumPopup(new GUIContent("Mouse Input Inversion", "Which axes of the mouse input should be inverted if any?"), t.mouseInputInversion);
                t.Sensitivity = EditorGUILayout.Slider(new GUIContent("Mouse Sensitivity", "Sensitivity of the mouse"), t.Sensitivity, 1, 20);
                t.rotationWeight = EditorGUILayout.Slider(new GUIContent("Camera Weight", "How heavy should the camera feel?"), t.rotationWeight, 1, 25);
                t.verticalRotationRange = EditorGUILayout.Slider(new GUIContent("Vertical Rotation Range", "The vertical angle range (In degrees) that the camera is allowed to move in"), t.verticalRotationRange, 1, 180);

                t.lockAndHideMouse = EditorGUILayout.ToggleLeft(new GUIContent("Lock and Hide mouse Cursor", "Should the controller lock and hide the cursor?"), t.lockAndHideMouse);
                t.autoGenerateCrosshair = EditorGUILayout.ToggleLeft(new GUIContent("Auto Generate Crosshair", "Should the controller automatically generate a crosshair?"), t.autoGenerateCrosshair);
                GUI.enabled = t.autoGenerateCrosshair;
                t.crosshairSprite = (Sprite)EditorGUILayout.ObjectField(new GUIContent("Crosshair Sprite", "The Sprite the controller will use when generating a crosshair."), t.crosshairSprite, typeof(Sprite), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                t.showCrosshairIn3rdPerson = EditorGUILayout.ToggleLeft(new GUIContent("Show Crosshair in 3rd person?", "Should the controller show the crosshair in 3rd person?"), t.showCrosshairIn3rdPerson);
                GUI.enabled = true;
                t.drawPrimitiveUI = EditorGUILayout.ToggleLeft(new GUIContent("Draw Primitive UI", "Should the controller automatically generate and draw primitive stat UI?"), t.drawPrimitiveUI);
                EditorGUILayout.Space(20);

                if (t.cameraPerspective == PerspectiveModes._1stPerson)
                {
                    t.viewInputMethods = (ViewInputModes)EditorGUILayout.EnumPopup(new GUIContent("Camera Input Methods", "The input method used to rotate the camera."), t.viewInputMethods);
                    t.standingEyeHeight = EditorGUILayout.Slider(new GUIContent("Standing Eye Height", "The Eye height of the player measured from the center of the character's capsule and upwards."), t.standingEyeHeight, 0, 1);
                    t.crouchingEyeHeight = EditorGUILayout.Slider(new GUIContent("Crouching Eye Height", "The Eye height of the player measured from the center of the character's capsule and upwards."), t.crouchingEyeHeight, 0, 1);
                    t.FOVKickAmount = EditorGUILayout.Slider(new GUIContent("FOV Kick Amount", "How much should the camera's FOV change based on the current movement speed?"), t.FOVKickAmount, 0, 50);
                    t.FOVSensitivityMultiplier = EditorGUILayout.Slider(new GUIContent("FOV Sensitivity Multiplier", "How much should the camera's FOV effect the mouse sensitivity? (Lower FOV = less sensitive)"), t.FOVSensitivityMultiplier, 0, 1);
                }
                else
                {
                    t.rotateCharacterToCameraForward = EditorGUILayout.ToggleLeft(new GUIContent("Rotate Ungrounded Character to Camera Forward", "Should the character get rotated towards the camera's forward facing direction when mid air?"), t.rotateCharacterToCameraForward);
                    t.standingEyeHeight = EditorGUILayout.Slider(new GUIContent("Head Height", "The Head height of the player measured from the center of the character's capsule and upwards."), t.standingEyeHeight, 0, 1);
                    t.maxCameraDistance = EditorGUILayout.Slider(new GUIContent("Max Camera Distance", "The farthest distance the camera is allowed to hover from the character's head"), t.maxCameraDistance, 0, 15);
                    t.cameraZoomSensitivity = EditorGUILayout.Slider(new GUIContent("Camera Zoom Sensitivity", "How sensitive should the mouse scroll wheel be when zooming the camera in and out?"), t.cameraZoomSensitivity, 1, 5);
                    t.bodyCatchupSpeed = EditorGUILayout.Slider(new GUIContent("Body Mesh Alignment Speed", "How quickly will the body align itself with the camera's relative direction"), t.bodyCatchupSpeed, 0, 5);
                    t.inputResponseFiltering = EditorGUILayout.Slider(new GUIContent("Input Response Filtering", "How quickly will the internal input direction align itself the player's input"), t.inputResponseFiltering, 0, 5);
                    EditorGUILayout.PropertyField(obstructionMaskField, new GUIContent("Camera Obstruction Layers", "The Layers the camera will register as an obstruction and move in front of ."));
                }
            }
            EditorGUILayout.Space();
            cameraSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(cameraSettingsFoldout, cameraSettingsFoldout ? "<color=#B83C82>show less</color>" : "<color=#B83C82>show more</color>", ShowMoreStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
            if (GUI.changed) { EditorUtility.SetDirty(t); Undo.RecordObject(t, "Undo Camera Setting changes"); tSO.ApplyModifiedProperties(); }
            #endregion

            #region Movement Settings

            EditorGUILayout.Space(); EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); EditorGUILayout.Space();
            GUILayout.Label("Movement Settings", labelHeaderStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(20);

            EditorGUILayout.BeginVertical(BoxPanel);
            if (movementSettingFoldout)
            {
                #region Stances and Speed
                t.enableMovementControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Movement", "Should the player have control over the character's movement?"), t.enableMovementControl);
                GUILayout.Label("<color=grey>Stances and Speed</color>", labelSubHeaderStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginVertical(BoxPanel);
                EditorGUILayout.Space(15);

                GUI.enabled = false;
                t.currentGroundMovementSpeed = (GroundSpeedProfiles)EditorGUILayout.EnumPopup(new GUIContent("Current Movement Speed", "Displays the player's current movement speed"), t.currentGroundMovementSpeed);
                GUI.enabled = true;

                EditorGUILayout.Space();
                t.walkingSpeed = EditorGUILayout.Slider(new GUIContent("Walking Speed", "How quickly can the player move while walking?"), t.walkingSpeed, 1, 400);

                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(BoxPanel);
                t.canSprint = EditorGUILayout.ToggleLeft(new GUIContent("Can Sprint", "Is the player allowed to enter a sprint?"), t.canSprint);
                GUI.enabled = t.canSprint;
                t.toggleSprint = EditorGUILayout.ToggleLeft(new GUIContent("Toggle Sprint", "Should the spring key act as a toggle?"), t.toggleSprint);
#if ENABLE_INPUT_SYSTEM
            t.sprintKey = (Key)EditorGUILayout.EnumPopup(new GUIContent("Sprint Key", "The Key used to enter a sprint."),t.sprintKey);
#else
                t.sprintKey_L = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Sprint Key", "The Key used to enter a sprint."), t.sprintKey_L);
#endif
                t.sprintingSpeed = EditorGUILayout.Slider(new GUIContent("Sprinting Speed", "How quickly can the player move while sprinting?"), t.sprintingSpeed, t.walkingSpeed + 1, 650);
                t.decelerationSpeed = EditorGUILayout.Slider(new GUIContent("Deceleration Factor", "Behaves somewhat like a braking force"), t.decelerationSpeed, 1, 300);
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(BoxPanel);
                t.canCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Can Crouch", "Is the player allowed to crouch?"), t.canCrouch);
                GUI.enabled = t.canCrouch;
                t.toggleCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Toggle Crouch", "Should pressing the crouch button act as a toggle?"), t.toggleCrouch);
#if ENABLE_INPUT_SYSTEM
            t.crouchKey= (Key)EditorGUILayout.EnumPopup(new GUIContent("Crouch Key", "The Key used to start a crouch."),t.crouchKey);
#else
                t.crouchKey_L = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Crouch Key", "The Key used to start a crouch."), t.crouchKey_L);
#endif
                t.crouchingSpeed = EditorGUILayout.Slider(new GUIContent("Crouching Speed", "How quickly can the player move while crouching?"), t.crouchingSpeed, 1, t.walkingSpeed - 1);
                t.crouchingHeight = EditorGUILayout.Slider(new GUIContent("Crouching Height", "How small should the character's capsule collider be when crouching?"), t.crouchingHeight, 0.01f, 2);
                EditorGUILayout.EndVertical();

                GUI.enabled = true;


                EditorGUILayout.Space(20);
                GUI.enabled = false;
                t.currentStance = (Stances)EditorGUILayout.EnumPopup(new GUIContent("Current Stance", "Displays the character's current stance"), t.currentStance);
                GUI.enabled = true;
                t.stanceTransitionSpeed = EditorGUILayout.Slider(new GUIContent("Stance Transition Speed", "How quickly should the character change stances?"), t.stanceTransitionSpeed, 0.1f, 10);

                EditorGUILayout.PropertyField(groundLayerMask, new GUIContent("What Is Ground", "What physics layers should be considered to be ground?"));

                #region Slope affectors
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(BoxPanel);
                GUILayout.Label("<color=grey>Slope Affectors</color>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 10, richText = true }, GUILayout.ExpandWidth(true));

                t.hardSlopeLimit = EditorGUILayout.Slider(new GUIContent("Hard Slope Limit", "At what slope angle should the player no longer be able to walk up?"), t.hardSlopeLimit, 45, 89);
                t.maxStairRise = EditorGUILayout.Slider(new GUIContent("Maximum Stair Rise", "How tall can a single stair rise?"), t.maxStairRise, 0, 1.5f);
                t.stepUpSpeed = EditorGUILayout.Slider(new GUIContent("Step Up Speed", "How quickly will the player climb a step?"), t.stepUpSpeed, 0.01f, 0.45f);
                EditorGUILayout.EndVertical();
                #endregion
                EditorGUILayout.EndVertical();
                #endregion

                #region Jumping
                EditorGUILayout.Space();
                GUILayout.Label("<color=grey>Jumping Settings</color>", labelSubHeaderStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginVertical(BoxPanel);
                //EditorGUILayout.Space(15);

                t.canJump = EditorGUILayout.ToggleLeft(new GUIContent("Can Jump", "Is the player allowed to jump?"), t.canJump);
                GUI.enabled = t.canJump;
#if ENABLE_INPUT_SYSTEM
            t.jumpKey = (Key)EditorGUILayout.EnumPopup(new GUIContent("Jump Key", "The Key used to jump."),t.jumpKey);
#else
                t.jumpKey_L = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Jump Key", "The Key used to jump."), t.jumpKey_L);
#endif
                t.holdJump = EditorGUILayout.ToggleLeft(new GUIContent("Continuous Jumping", "Should the player be able to continue jumping without letting go of the Jump key"), t.holdJump);
                t.jumpPower = EditorGUILayout.Slider(new GUIContent("Jump Power", "How much power should a jump have?"), t.jumpPower, 1, 650f);
                t.airControlFactor = EditorGUILayout.Slider(new GUIContent("Air Control Factor", "EXPERIMENTAL: How much control should the player have over their direction while in the air"), t.airControlFactor, 0, 1);
                GUI.enabled = t.enableStaminaSystem;
                t.jumpingDepletesStamina = EditorGUILayout.ToggleLeft(new GUIContent("Jumping Depletes Stamina", "Should jumping deplete stamina?"), t.jumpingDepletesStamina);
                t.s_JumpStaminaDepletion = EditorGUILayout.Slider(new GUIContent("Jump Stamina Depletion Amount", "How much stamina should jumping use?"), t.s_JumpStaminaDepletion, 0, t.Stamina);
                t.s_FacaStaminaDepletion = EditorGUILayout.Slider(new GUIContent("Facazo Stamina Depletion Amount", "How much stamina should Faca use?"), t.s_FacaStaminaDepletion, 0, t.Stamina);
                GUI.enabled = true;
                t.jumpEnhancements = EditorGUILayout.ToggleLeft(new GUIContent("Enable Jump Enhancements", "Should extra math be used to enhance the jump curve?"), t.jumpEnhancements);
                if (t.jumpEnhancements)
                {
                    t.decentMultiplier = EditorGUILayout.Slider(new GUIContent("On Decent Multiplier", "When the player begins to descend  during a jump, what should gravity be multiplied by?"), t.decentMultiplier, 0.1f, 5);
                    t.tapJumpMultiplier = EditorGUILayout.Slider(new GUIContent("Tap Jump Multiplier", "When the player lets go of space prematurely during a jump, what should gravity be multiplied by?"), t.tapJumpMultiplier, 0.1f, 5);
                }

                EditorGUILayout.EndVertical();
                #endregion

                #region Sliding
                EditorGUILayout.Space();
                GUILayout.Label("<color=grey>Sliding Settings</color>", labelSubHeaderStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginVertical(BoxPanel);
                //EditorGUILayout.Space(15);

                t.canSlide = EditorGUILayout.ToggleLeft(new GUIContent("Can Slide", "Is the player allowed to slide? Use the crouch key to initiate a slide!"), t.canSlide);
                GUI.enabled = t.canSlide;
#if ENABLE_INPUT_SYSTEM
            t.slideKey = (Key)EditorGUILayout.EnumPopup(new GUIContent("Slide Key", "The Key used to Slide while the character is sprinting."),t.slideKey);
#else
                t.slideKey_L = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Slide Key", "The Key used to Slide wile the character is sprinting."), t.slideKey_L);
#endif
                t.slidingDeceleration = EditorGUILayout.Slider(new GUIContent("Sliding Deceleration", "How much deceleration should be applied while sliding?"), t.slidingDeceleration, 50, 300);
                t.slidingTransitionSpeed = EditorGUILayout.Slider(new GUIContent("Sliding Transition Speed", "How quickly should the character transition from the current stance to sliding?"), t.slidingTransitionSpeed, 0.01f, 10);
                t.maxFlatSlideDistance = EditorGUILayout.Slider(new GUIContent("Flat Slide Distance", "If the player starts sliding on a flat surface with no ground angle influence, How many units should the player slide forward?"), t.maxFlatSlideDistance, 0.5f, 15);
                GUI.enabled = true;
                EditorGUILayout.EndVertical();
                #endregion

                if (GUI.changed) { EditorUtility.SetDirty(t); Undo.RecordObject(t, "Undo Movement Setting changes"); tSO.ApplyModifiedProperties(); }
            }
            else
            {
                t.enableMovementControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Movement", "Should the player have control over the character's movement?"), t.enableMovementControl);
                t.walkingSpeed = EditorGUILayout.Slider(new GUIContent("Walking Speed", "How quickly can the player move while walking?"), t.walkingSpeed, 1, 400);
                t.sprintingSpeed = EditorGUILayout.Slider(new GUIContent("Sprinting Speed", "How quickly can the player move while sprinting?"), t.sprintingSpeed, t.walkingSpeed + 1, 650);
            }
            EditorGUILayout.Space();
            movementSettingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(movementSettingFoldout, movementSettingFoldout ? "<color=#B83C82>show less</color>" : "<color=#B83C82>show more</color>", ShowMoreStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion

            #region Stamina
            EditorGUILayout.Space(); EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); EditorGUILayout.Space();
            GUILayout.Label("Stamina", labelHeaderStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(BoxPanel);
            t.enableStaminaSystem = EditorGUILayout.ToggleLeft(new GUIContent("Enable Stamina System", "Should the controller enable it's stamina system?"), t.enableStaminaSystem);

            //preview bar
            Rect casingRectSP = EditorGUILayout.GetControlRect(),
                    statRectSP = new Rect(casingRectSP.x + 2, casingRectSP.y + 2, Mathf.Clamp(((casingRectSP.width / t.Stamina) * t.currentStaminaLevel) - 4, 0, casingRectSP.width), casingRectSP.height - 4),
                    statRectMSP = new Rect(casingRectSP.x + 2, casingRectSP.y + 2, Mathf.Clamp(((casingRectSP.width / t.Stamina) * t.s_minimumStaminaToSprint) - 4, 0, casingRectSP.width), casingRectSP.height - 4);
            EditorGUI.DrawRect(casingRectSP, statBackingColor);
            EditorGUI.DrawRect(statRectMSP, new Color32(96, 96, 64, 255));
            EditorGUI.DrawRect(statRectSP, new Color32(94, 118, 135, (byte)(GUI.enabled ? 191 : 64)));


            GUI.enabled = t.enableStaminaSystem;
            t.Stamina = EditorGUILayout.Slider(new GUIContent("Stamina", "The maximum stamina level"), t.Stamina, 0, 250.0f);
            t.s_minimumStaminaToSprint = EditorGUILayout.Slider(new GUIContent("Minimum Stamina To Sprint", "The minimum stamina required to enter a sprint."), t.s_minimumStaminaToSprint, 0, t.Stamina);
            t.s_depletionSpeed = EditorGUILayout.Slider(new GUIContent("Depletion Speed", ""), t.s_depletionSpeed, 0, 15.0f);
            t.s_regenerationSpeed = EditorGUILayout.Slider(new GUIContent("Regeneration Speed", "The speed at which stamina will regenerate"), t.s_regenerationSpeed, 0, 10.0f);

            GUI.enabled = true;
            EditorGUILayout.EndVertical();
            if (GUI.changed) { EditorUtility.SetDirty(t); Undo.RecordObject(t, "Undo Stamina Setting changes"); tSO.ApplyModifiedProperties(); }
            #endregion

            #region Footstep Audio
            EditorGUILayout.Space(); EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); EditorGUILayout.Space();
            GUILayout.Label("Footstep Audio", labelHeaderStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(BoxPanel);

            t.enableFootstepSounds = EditorGUILayout.ToggleLeft(new GUIContent("Enable Footstep System", "Should the crontoller enable it's footstep audio systems?"), t.enableFootstepSounds);
            GUI.enabled = t.enableFootstepSounds;
            t.footstepTriggeringMode = (FootstepTriggeringMode)EditorGUILayout.EnumPopup(new GUIContent("Footstep Trigger Mode", "How should a footstep SFX call be triggered? \n\n- Calculated Timing: The controller will attempt to calculate the footstep cycle position based on Headbob cycle position, movement speed, and capsule size. This can sometimes be inaccurate depending on the selected perspective and base walk speed. (Not recommended if character animations are being used)\n\n- Called From Animations: The controller will not do it's own footstep cycle calculations/call for SFX. Instead the controller will rely on character Animations to call the 'CallFootstepClip()' function. This gives much more precise results. The controller will still calculate what footstep clips should be played."), t.footstepTriggeringMode);

            if (t.footstepTriggeringMode == FootstepTriggeringMode.calculatedTiming)
            {
                t.stepTiming = EditorGUILayout.Slider(new GUIContent("Step Timing", "The time (measured in seconds) between each footstep."), t.stepTiming, 0.0f, 1.0f);
                t.modificadorCorriendo = EditorGUILayout.Slider(new GUIContent("Modificador Corriendo", "The time (measured in seconds) between each footstep."), t.modificadorCorriendo, 0.0f, 1.0f);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.Space();
            //GUILayout.Label("<color=grey>Clip Stacks</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
            EditorGUI.indentLevel++;
            footStepFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(footStepFoldout, footStepFoldout ? "<color=#B83C82>hide clip stacks</color>" : "<color=#B83C82>show clip stacks</color>", ShowMoreStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel--;
            if (footStepFoldout)
            {
                if (t.footstepSoundSet.Any())
                {
                    if (!Application.isPlaying)
                    {
                        for (int i = 0; i < groundMatProf.arraySize; i++)
                        {
                            EditorGUILayout.BeginVertical(BoxPanel);
                            EditorGUILayout.BeginVertical(BoxPanel);

                            SerializedProperty profile = groundMatProf.GetArrayElementAtIndex(i), clipList = profile.FindPropertyRelative("footstepClips"), mat = profile.FindPropertyRelative("_Materials"), physMat = profile.FindPropertyRelative("_physicMaterials"), layer = profile.FindPropertyRelative("_Layers"), triggerType = profile.FindPropertyRelative("profileTriggerType");
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"Clip Stack {i + 1}", clipSetLabelStyle);
                            if (GUILayout.Button(new GUIContent("X", "Remove this profile"), GUILayout.MaxWidth(20))) { t.footstepSoundSet.RemoveAt(i); UpdateGroundProfiles(); break; }
                            EditorGUILayout.EndHorizontal();

                            //Check again that the list of profiles isn't empty incase we removed the last one with the button above.
                            if (t.footstepSoundSet.Any())
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.PropertyField(triggerType, new GUIContent("Trigger Mode", "Is this clip stack triggered by a Material or a Terrain Layer?"));
                                switch (t.footstepSoundSet[i].profileTriggerType)
                                {
                                    case MatProfileType.Material: { EditorGUILayout.PropertyField(mat, new GUIContent("Materials", "The materials used to trigger this footstep stack.")); } break;
                                    case MatProfileType.physicMaterial: { EditorGUILayout.PropertyField(physMat, new GUIContent("Physic Materials", "The Physic Materials used to trigger this footstep stack.")); } break;
                                    case MatProfileType.terrainLayer: { EditorGUILayout.PropertyField(layer, new GUIContent("Terrain Layers", "The Terrain Layers used to trigger this footstep stack.")); } break;
                                }
                                EditorGUILayout.Space();

                                EditorGUILayout.PropertyField(clipList, new GUIContent("Clip Stack", "The Audio clips used in this stack."), true);
                                EditorGUI.indentLevel--;
                                EditorGUILayout.EndVertical();
                                EditorGUILayout.EndVertical();
                                EditorGUILayout.Space();
                                if (GUI.changed) { EditorUtility.SetDirty(t); Undo.RecordObject(t, $"Undo changes to Clip Stack {i + 1}"); tSO.ApplyModifiedProperties(); }
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Foot step sound sets hidden to save runtime resources.", MessageType.Info);
                    }
                }
                if (GUILayout.Button(new GUIContent("Add Profile", "Add new profile"))) { t.footstepSoundSet.Add(new GroundMaterialProfile() { profileTriggerType = MatProfileType.Material, _Materials = null, _Layers = null, footstepClips = new List<AudioClip>() }); UpdateGroundProfiles(); }
                if (GUILayout.Button(new GUIContent("Remove All Profiles", "Remove all profiles"))) { t.footstepSoundSet.Clear(); }
                EditorGUILayout.Space();
                footStepFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(footStepFoldout, footStepFoldout ? "<color=#B83C82>hide clip stacks</color>" : "<color=#B83C82>show clip stacks</color>", ShowMoreStyle);
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            //EditorGUILayout.PropertyField(groundMatProf,new GUIContent("Footstep Sound Profiles"));

            GUI.enabled = true;
            //EditorGUILayout.HelpBox("Due to limitations In order to use the Material trigger mode, Imported Mesh's must have Read/Write enabled. Additionally, these Mesh's cannot be marked as Batching Static. Work arounds for both of these limitations are being researched.", MessageType.Info);
            EditorGUILayout.EndVertical();
            if (GUI.changed) { EditorUtility.SetDirty(t); Undo.RecordObject(t, "Undo Footstep Audio Setting changes"); tSO.ApplyModifiedProperties(); }

            #endregion



            #region Animation Triggers
            EditorGUILayout.Space(); EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); EditorGUILayout.Space();
            GUILayout.Label("Animator Settup", labelHeaderStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(BoxPanel);
            //t._1stPersonCharacterAnimator = (Animator)EditorGUILayout.ObjectField(new GUIContent("1st Person Animator", "The animator used on the 1st person character mesh (if any)"), t._1stPersonCharacterAnimator, typeof(Animator), true);
            t._3rdPersonCharacterAnimator = (Animator)EditorGUILayout.ObjectField(new GUIContent("3rd Person Animator", "The animator used on the 3rd person character mesh (if any)"), t._3rdPersonCharacterAnimator, typeof(Animator), true);
            if (t._3rdPersonCharacterAnimator /*|| t._1stPersonCharacterAnimator*/)
            {
                EditorGUILayout.BeginVertical(BoxPanel);
                GUILayout.Label("Parameters", labelSubHeaderStyle, GUILayout.ExpandWidth(true));
                t.a_velocity = EditorGUILayout.TextField(new GUIContent("Velocity (Float)", "(Float) The name of the Velocity Parameter in the animator"), t.a_velocity);
                t.a_2DVelocity = EditorGUILayout.TextField(new GUIContent("2D Velocity (Float)", "(Float) The name of the 2D Velocity Parameter in the animator"), t.a_2DVelocity);
                t.a_Idle = EditorGUILayout.TextField(new GUIContent("Idle (Bool)", "(Bool) The name of the Idle Parameter in the animator"), t.a_Idle);
                t.a_Sprinting = EditorGUILayout.TextField(new GUIContent("Sprinting (Bool)", "(Bool) The name of the Sprinting Parameter in the animator"), t.a_Sprinting);
                t.a_Crouching = EditorGUILayout.TextField(new GUIContent("Crouching (Bool)", "(Bool) The name of the Crouching Parameter in the animator"), t.a_Crouching);
                t.a_Sliding = EditorGUILayout.TextField(new GUIContent("Sliding (Bool)", "(Bool) The name of the Sliding Parameter in the animator"), t.a_Sliding);
                t.a_Jumped = EditorGUILayout.TextField(new GUIContent("Jumped (Bool)", "(Bool) The name of the Jumped Parameter in the animator"), t.a_Jumped);
                t.a_Grounded = EditorGUILayout.TextField(new GUIContent("Grounded (Bool)", "(Bool) The name of the Grounded Parameter in the animator"), t.a_Grounded);
                t.a_facon = EditorGUILayout.TextField(new GUIContent("Facon (Bool)", "(Bool) Facon"), t.a_facon);
                t.a_faconazo = EditorGUILayout.TextField(new GUIContent("Facon (Trigger)", "(Trigger"), t.a_faconazo);
                t.a_esquivar = EditorGUILayout.TextField(new GUIContent("Esquivar (Trigger)", "(Trigger"), t.a_esquivar);
                t.a_poncho = EditorGUILayout.TextField(new GUIContent("Poncho (Bool)", "(Bool"), t.a_poncho);
                t.a_velXZ = EditorGUILayout.TextField(new GUIContent("velXZ (Float)", "(Float)"), t.a_velXZ);
                t.a_rifle = EditorGUILayout.TextField(new GUIContent("Rifle (Bool)", "(Bool) Rifle"), t.a_rifle);
                t.a_boleadoras = EditorGUILayout.TextField(new GUIContent("Boleadoras (Bool)", "(Bool) Boleadoras"), t.a_boleadoras);
                t.a_VelX = EditorGUILayout.TextField(new GUIContent("VelX (Float)", "(Float)"), t.a_VelX);
                t.a_VelY = EditorGUILayout.TextField(new GUIContent("VelY (Float)", "(Float)"), t.a_VelY);
                t.a_lanzar = EditorGUILayout.TextField(new GUIContent("LanzarB (Trigger)", "(Trigger"), t.a_lanzar);
                //t.a_isDeath = EditorGUILayout.TextField(new GUIContent("isDeath (Trigger)", "(Trigger"), t.a_isDeath);
                t.a_isDeath = EditorGUILayout.TextField(new GUIContent("IsDeath (Bool)", "(Bool) IsDeath"), t.a_isDeath);
                EditorGUILayout.EndVertical();
            }
            //EditorGUILayout.HelpBox("WIP - This is a work in progress feature and currently very primitive.\n\n No triggers, bools, floats, or ints are set up in the script. To utilize this feature, find 'UpdateAnimationTriggers()' function in this script and set up triggers with the correct string names there. This function gets called by the script whenever a relevant parameter gets updated. (I.e. when 'isVaulting' changes)", MessageType.Info);
            EditorGUILayout.EndVertical();
            if (GUI.changed) { EditorUtility.SetDirty(t); Undo.RecordObject(t, "Undo Animation settings changes"); tSO.ApplyModifiedProperties(); }
            #endregion

        }

        void UpdateGroundProfiles()
        {
            tSO = new SerializedObject(t);
            groundMatProf = tSO.FindProperty("footstepSoundSet");
        }
    }
#endif
    #endregion

}
