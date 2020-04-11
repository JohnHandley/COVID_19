using System;
using System.Collections;
using UnityEngine;

public class PlayerMovement : Singleton<PlayerMovement>
{
    public enum GameState
    {
        FREE,
        STARFE
    }

    GameState currentState = GameState.FREE;

    [SerializeField]
    Events.EventGameState onGameStateChange = new Events.EventGameState();

    Camera mainCamera;
    Animator animator;
    CharacterController playerController;
    Vector2 inputVector = Vector2.zero;
    Vector2 rawInputVector = Vector2.zero;
    CommonRepo commonRepo;

    [Range(0, 1)]
    [SerializeField]
    float speedDampTime = 0.15f;
    [Range(0, 1)]
    [SerializeField]
    float directionDampTime = 0.15f;
    [SerializeField]
    float directionSpeed = 3f;
    [Range(0f, 1f)]
    [SerializeField]
    float sprintDampTime = 0.15f;
    [Range(0f, 1f)]
    [SerializeField]
    float floorAngleDampTime = 0.15f;
    [Range(0f, 1f)]
    [SerializeField]
    float slopeDetectionDistance = 0.1f;
    [Range(0f, 100f)]
    [SerializeField]
    float onSlopeExtraDownForce = 5f;
    [Range(0f, 5f)]
    [SerializeField]
    float pushForce = 5f;
    [Range(0f, 1f)]
    [SerializeField]
    float rotationSpeed = 0.1f;

    float angle = 0f;
    Vector3 fallVelocity = Vector3.zero;
    Vector3 cameraDirection = Vector3.zero;
    bool isGroundReset = true;

    int leftFootUpHash;
    int rightFootUpHash;
    int directionHash;
    int angleHash;
    int speedHash;
    int rawSpeedHash;
    int sprintFactorHash;
    int groundedFootHash;
    int floorAngleHash;
    int isGroundedHash;
    int isFreeHash;
    int isCrouchedHash;
    int fallDistanceHash;
    int freeHash;
    int starfeHash;
    int crouchHash;
    int standHash;
    int rightHash;
    int forwardHash;
    int turnBlendPathHash;
    int crouchLocomotionPathHash;
    int crouchStarfeTurnBlendPathHash;

    void Start()
    {
        mainCamera = Camera.main;
        animator = GetComponent<Animator>();
        playerController = GetComponent<CharacterController>();
        commonRepo = CommonRepo.instance;

        //Set Animator Hashes
        leftFootUpHash = Animator.StringToHash(StaticStrings.RightFootUp);
        rightFootUpHash = Animator.StringToHash(StaticStrings.LeftFootUp);
        directionHash = Animator.StringToHash(StaticStrings.Direction);
        angleHash = Animator.StringToHash(StaticStrings.Angle);
        speedHash = Animator.StringToHash(StaticStrings.Speed);
        rawSpeedHash = Animator.StringToHash(StaticStrings.RawSpeed);
        sprintFactorHash = Animator.StringToHash(StaticStrings.SprintFactor);
        groundedFootHash = Animator.StringToHash(StaticStrings.GroundedFoot);
        floorAngleHash = Animator.StringToHash(StaticStrings.FloorAngle);
        isGroundedHash = Animator.StringToHash(StaticStrings.IsGrounded);
        isFreeHash = Animator.StringToHash(StaticStrings.IsFree);
        isCrouchedHash = Animator.StringToHash(StaticStrings.IsCrouched);
        fallDistanceHash = Animator.StringToHash(StaticStrings.FallDistance);
        freeHash = Animator.StringToHash(StaticStrings.Free);
        starfeHash = Animator.StringToHash(StaticStrings.Starfe);
        crouchHash = Animator.StringToHash(StaticStrings.Crouch);
        standHash = Animator.StringToHash(StaticStrings.Stand);
        rightHash = Animator.StringToHash(StaticStrings.Right);
        forwardHash = Animator.StringToHash(StaticStrings.Forward);
        turnBlendPathHash = Animator.StringToHash(StaticStrings.TurnBlendFullPath);
        crouchLocomotionPathHash = Animator.StringToHash(StaticStrings.CrouchLocomotionFullPath);
        crouchStarfeTurnBlendPathHash = Animator.StringToHash(StaticStrings.CrouchStarfeTurnBlendFullPath);

        onGameStateChange.AddListener(OnGameStateChange);
        
        //Lock Cursor
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        inputVector = commonRepo.InputVector;
        rawInputVector = commonRepo.RawInputVector;

        //State Change Logic
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            UpdateGameState(currentState.Equals(GameState.FREE) ? GameState.STARFE : GameState.FREE);
        }

        animator.SetBool(isFreeHash, currentState.Equals(GameState.FREE));
        animator.SetBool(isCrouchedHash, Input.GetButton(StaticStrings.Crouch));

        //Stance Change Logic
        if (Input.GetButtonDown(StaticStrings.Crouch)
            ||
            Input.GetButtonUp(StaticStrings.Crouch))
        {
            animator.SetTrigger(Input.GetButtonUp(StaticStrings.Crouch) ? standHash : crouchHash);
        }

        animator.SetFloat(directionHash, CalculateDirection(), directionDampTime, Time.deltaTime);
        animator.SetFloat(speedHash, CalculateSpeed(), speedDampTime, Time.deltaTime);
        animator.SetFloat(rawSpeedHash, CalculateRawSpeed());

        if (currentState.Equals(GameState.STARFE))
        {
            animator.SetFloat(rightHash, inputVector.normalized.x, speedDampTime, Time.deltaTime);
            animator.SetFloat(forwardHash, inputVector.normalized.y, speedDampTime, Time.deltaTime);
        }

        //Angle Calculation is done in direction Calculation
        animator.SetFloat(angleHash, angle, speedDampTime, Time.deltaTime);

        //SprintFactor Calculation is done in speed Calculation
        animator.SetFloat(sprintFactorHash, Input.GetButton(StaticStrings.Sprint) ? 1 : 0.5f, sprintDampTime, Time.deltaTime);

        animator.SetFloat(floorAngleHash, GetFloorAngle(), floorAngleDampTime, Time.deltaTime);

        if (GetFloorAngle() > 5f && !inputVector.Equals(Vector2.zero))
        {
            playerController.Move(Vector3.down * GetFloorAngle() * onSlopeExtraDownForce * Time.deltaTime);
        }

        //This should be executed after Ground Checking is done in Update
        animator.SetBool(isGroundedHash, playerController.isGrounded);
        
        //Calculates fallVelocity.y
        CalculateFallDisplacement();

        animator.SetFloat(fallDistanceHash, fallVelocity.y, 0f, Time.deltaTime);

        animator.SetFloat(groundedFootHash, 
            playerController.isGrounded ? 
            animator.GetFloat(leftFootUpHash) - animator.GetFloat(rightFootUpHash) : animator.GetFloat(groundedFootHash), 
            0f, Time.deltaTime);


        //Add Fall Forward Force when Airborne
        if (!playerController.isGrounded && inputVector != Vector2.zero)
        {
            playerController.Move(transform.forward * pushForce * animator.GetFloat(speedHash) * (animator.GetFloat(sprintFactorHash) > 0.5f ? 2f : 1f) * Time.deltaTime);
        }

        if (currentState.Equals(GameState.STARFE))
        {
            //Rotate with Mouse ... cameraDirection is Calculated in CalculateDirection()
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(cameraDirection),
                //Set Rotation Speed For Different States
                animator.GetCurrentAnimatorStateInfo(0).fullPathHash.Equals(turnBlendPathHash) 
                || animator.GetCurrentAnimatorStateInfo(0).fullPathHash.Equals(crouchStarfeTurnBlendPathHash) 
                ? 0f : rotationSpeed);
        }

        else if (currentState.Equals(GameState.FREE))
        {
            Vector3 forward = mainCamera.transform.forward;
            Vector3 right = mainCamera.transform.right;
            forward.y = 0;
            right.y = 0;

            //Rotate towards inputVector
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.LookRotation((right.normalized * inputVector.x) + (forward.normalized * inputVector.y)),
                //Set Rotation Speed For Different States
                animator.GetCurrentAnimatorStateInfo(0).fullPathHash.Equals(crouchLocomotionPathHash) 
                && !rawInputVector.Equals(Vector2.zero) 
                ? rotationSpeed : 0f);
        }
    }

    float CalculateSpeed()
    {
        float speed;

        inputVector.Normalize();

        speed = currentState.Equals(GameState.FREE) ? (inputVector.magnitude * (Input.GetButton(StaticStrings.Walk) ? 0.5f : 1f)) : inputVector.magnitude;

        return speed;
    }

    float CalculateRawSpeed()
    {
        float rawSpeed;

        rawInputVector.Normalize();

        rawSpeed = currentState.Equals(GameState.FREE) ? (rawInputVector.magnitude * (Input.GetButton(StaticStrings.Walk) ? 0.5f : 1f)) : rawInputVector.magnitude;

        return rawSpeed;
    }

    float CalculateDirection()
    {
        Vector3 stickDirection = currentState.Equals(GameState.FREE) ? new Vector3(inputVector.x, 0, inputVector.y) : Vector3.forward;

        //Camera Rotation
        cameraDirection = mainCamera.transform.forward;

        cameraDirection.y = 0f;
        cameraDirection.Normalize();

        Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, cameraDirection);

        //Input to Worldspace coordinates
        Vector3 moveDirection = referentialShift * stickDirection;
        Vector3 axisSign = Vector3.Cross(moveDirection, transform.forward);

        float angleRootToMove = Vector3.Angle(transform.forward, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);

        angle = angleRootToMove;

        angleRootToMove /= 180;

        return angleRootToMove * directionSpeed;
    }

    float GetFloorAngle()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, slopeDetectionDistance))
        {
            return Vector3.Angle(hit.normal, transform.up);
        }

        return 0f;
    }

    void UpdateGameState(GameState state)
    {
        if (currentState.Equals(state))
        {
            return;
        }

        GameState previousState;

        previousState = currentState;
        currentState = state;

        onGameStateChange.Invoke(previousState, currentState);
    }

    void CalculateFallDisplacement()
    {
        if (playerController.isGrounded)
        {
            if (!isGroundReset)
            {
                StartCoroutine(ResetGround());

                isGroundReset = true;
            }
        }

        else
        {
            fallVelocity.y += Physics.gravity.y * Time.deltaTime;

            isGroundReset = false;
        }
    }

    IEnumerator ResetGround()
    {
        yield return new WaitForSeconds(0.25f);
        
        fallVelocity.y = 0f;
    }

    private void OnGameStateChange(GameState previousState, GameState currentState)
    {
        animator.SetTrigger(currentState.Equals(GameState.FREE) ? freeHash : starfeHash);
    }
}
