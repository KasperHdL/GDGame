﻿using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using KInput;
using UnityEngine.SceneManagement;

public class PlayerMovement : CharacterMovement
{
    public PostProcessingAnimator ppAnimator;
    public int health = 4;

    [Header("Audio Properties")]
    public float stepVolume = 0.1f;
    public bool fallOnWalls;
    public bool Inside = true;

    public float enemyColVol = 0.5f;
    public float defColVol = 0.5f;
    private bool isPlayingSteps = false;


    [HideInInspector] public Vector2 viewDirection;
    private bool fallen;

    [Header("Controller Settings")] public bool useController;
    [Range(0, 1)] public float deadzone = 0.1f;
    private Controller controller;

    [Header("Ambient Light Chase Settings")] public Light AmbientLight;
    public float ChaseLightIntMultiplier = 5;
    public float ChaseLightRanMultiplier = 5;
    private float OriginalALightIntensity;
    private float OriginalALightRange;

    private SonarTool sonar;

    [Header("Flashlight")]
    public GameObject flashlight_prefab;
    public GameObject flashlight;

    [HideInInspector] public bool isDead = false;

    [Header("Death Properties")]
    public float deathFade = 3f;
    public float deathBodyMass = 1;
    public float deathBodyDrag = 1;
    public float deathBodyAngularDrag = 1;

    public SpriteRenderer spriteRenderer;
    public Sprite woundedSprite;
    public Transform sonarSprite;
    public float moveAmount = 1f;
    private float hitTime;
    private bool playedBreathingStops = true;

    private bool doingOutroScene = false;

    public void Awake()
    {

        Cursor.visible = false;
        var savesystem = GameObject.FindGameObjectWithTag("SaveSystem");
        if (savesystem != null ){

            
            SaveSystem save = savesystem.GetComponent<SaveSystem>();
            if (save.playerPickedUpSonar)
            {
                sonarSprite.gameObject.SetActive(true);
                GetComponent<SonarTool>().enabled = true;
            }
            if (save.PlayerPosition.HasValue){
                transform.position = save.PlayerPosition.Value;
            }
        }
    //    doNotNormalize = true;
    }

    public override void Start()
    {
        base.Start();
        sonar = GetComponent<SonarTool>();
        sonar.sonarSprite = sonarSprite;
        controller = GetComponent<ControllerContainer>().controller;
        if (ppAnimator == null)
            ppAnimator = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<PostProcessingAnimator>();

        if (AmbientLight)
        {
            OriginalALightIntensity = AmbientLight.intensity;
            OriginalALightRange = AmbientLight.range;
        }
    }

    void OnTriggerStay2D(Collider2D coll){
        if(coll.gameObject.tag == "Interactable"){
            if(
                Input.GetButtonDown("Use") || 
                controller.GetButtonDown(KInput.Button.BumperLeft) ||
                controller.GetButtonDown(KInput.Button.X) ||
                controller.GetButtonDown(KInput.Button.A) ||
                controller.GetButtonDown(KInput.Button.StickRightClick) ||
                controller.GetButtonDown(KInput.Button.StickLeftClick) ||
                controller.GetAxis(Axis.TriggerLeft) > 0.75f
                )
            coll.gameObject.GetComponent<Interactable>().Interact();

        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {

        //Debug.Log("player hit a " + collision.gameObject.tag + ", named " + collision.gameObject.name);

        switch (collision.gameObject.tag)
        {
            case "Wall":
                if (fallOnWalls)
                    SoundSystem.Play("wall collision", 1, defColVol);
                
                break;
            case "PickUp":
                Debug.Log("pick up!!!");
                SoundSystem.Play("pickUp");
                if (collision.gameObject.name == "SonarChargePU")
                    sonar.sonarChargeLeft += 200f;
                if (collision.gameObject.name == "SonarPU"){
                    gameObject.GetComponent<SonarTool>().enabled = true;
                    SoundSystem.Play("charging",0.8f,0.5f);
                    sonarSprite.gameObject.SetActive(true);
                    GameObject.FindGameObjectWithTag("SaveSystem").GetComponent<SaveSystem>().playerPickedUpSonar = true;
                }
                Destroy(collision.gameObject);
                break;
            default:
                if (fallOnWalls)
                    SoundSystem.Play("default collision", defColVol);

                //Debug.Log("No collision tag set!");
                break;
        }
    }

    public void Hit(int damage)
    {
        if (isDead) return;
        DisableMovement = true;
        StartCoroutine(Reactivate());
        ppAnimator.PlayerAttacked();
        SoundSystem.Play("enemy collision", 1, enemyColVol);
        TrackingCamera.ShakeIt(0.5f, 0.5f);
        health -= damage;
        if (!SoundSystem.IsPlaying("breathing") && health > 0)
        {
            SoundSystem.Play("breathing",1,0.25f);
            playedBreathingStops = false;
        }
        hitTime = Time.time;
        spriteRenderer.sprite = woundedSprite;
        if (health <= 0) Die();
    }

    private IEnumerator Reactivate()
    {
        yield return new WaitForSeconds(0.1f);
        DisableMovement = false;
    }

    private void Die()
    {
        if (isDead || doingOutroScene) return;

// Drop Flashlight(does not look good currently)
//        GameObject fl = Instantiate(flashlight_prefab, flashlight.transform.position, flashlight.transform.rotation) as GameObject;
//       flashlight.GetComponent<Light>().enabled = false;

        body.mass = deathBodyMass;
        body.drag = deathBodyDrag;
        body.angularDrag = deathBodyAngularDrag;

        SoundSystem.Stop("footsteps");
        SoundSystem.Stop("snowsteps");
        SoundSystem.Stop("breathing");
        SoundSystem.Stop("breathingEnds");
        SoundSystem.Stop("Outside");

        isDead = true;

        playedBreathingStops = true;
        SoundSystem.Play("death", 1, 1, 0, deathFade - 0.75f);
        SoundSystem.Play("breathingEnds",1,0.55f,0, deathFade - 2f);

        ppAnimator.FadeToBlack();
        StartCoroutine(ReloadLevel(deathFade));
    }

    private IEnumerator ReloadLevel(float delay)
    {
        yield return new WaitForSeconds(delay);

        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void OutroScene(){
        sonar.enabled = false;
        body.velocity = Vector3.zero;
        body.drag = 10f;
        body.angularDrag = 5f;

        doingOutroScene = false;


        SoundSystem.Stop("footsteps");
        SoundSystem.Stop("snowsteps");
        SoundSystem.Stop("breathing");
        SoundSystem.Stop("breathingEnds");
        SoundSystem.Stop("Outside");


    }

    public override void Update()
    {

        if (isDead || doingOutroScene) return; 

        Vector3 v = Vector3.zero;
        if (useController)
        {
            v = new Vector3(controller.GetAxis(Axis.StickLeftX), controller.GetAxis(Axis.StickLeftY), 0);

            if (v.magnitude < deadzone)
            {
                v = Vector3.zero;
            }
            else
            {
                v = v.normalized*((v.magnitude - deadzone)/(1 - deadzone));
            }
        }

        if (v == Vector3.zero)
            v = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;

        Move = v;

        if (!isPlayingSteps)
        {
            if (body.velocity.magnitude >= 0.1)
            {
                if (Inside)
                {
                    SoundSystem.Play("footsteps", 1, stepVolume, 0, null, true);
                }
                else
                {
                    SoundSystem.Play("snowsteps", 1, stepVolume * 0.8f, 0, null, true);
                }

                isPlayingSteps = true;
            }
        }
        else if (body.velocity.magnitude < 0.1)
        {


            if (Time.time - hitTime > 2 &! playedBreathingStops && health > 0)
            {
                SoundSystem.Stop("breathing");
                SoundSystem.Play("breathingEnds",1,0.4f);
                playedBreathingStops = true;
            }
                
            SoundSystem.Stop("footsteps");
            SoundSystem.Stop("snowsteps");
            isPlayingSteps = false;
        }

        if (useController)
        {
            Vector2 d = new Vector2(controller.GetAxis(Axis.StickRightX), controller.GetAxis(Axis.StickRightY));
            if (d.magnitude < deadzone)
            {
                d = Vector2.zero;
            }
            else
            {
                d = d.normalized*((d.magnitude - deadzone)/(1 - deadzone));
            }

            if (d != Vector2.zero)
            {
                viewDirection = d;
            }
        }
        else
        {
            Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            viewDirection = mouse - (Vector2) transform.position;
            viewDirection = viewDirection.normalized;
        }

        transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg*Mathf.Atan2(viewDirection.y, viewDirection.x));


        if (AmbientLight != null)
        {
            var chase = GameObject.FindGameObjectsWithTag("Enemy").Any(e => e.GetComponent<FollowPlayer>().visible);
            AmbientLight.intensity = OriginalALightIntensity*(chase ? ChaseLightIntMultiplier : 1.0f);
            AmbientLight.range = OriginalALightRange*(chase ? ChaseLightRanMultiplier : 1.0f);
        }

        base.Update();
    }

    public void setInside(bool isInside)
    {
        if ((isInside & !Inside) || (!isInside && Inside))
        {
            SoundSystem.Stop("footsteps");
            SoundSystem.Stop("snowsteps");
            isPlayingSteps = false;
        }

        Inside = isInside;
    }
}
