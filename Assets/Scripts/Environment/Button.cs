﻿using UnityEngine;
using System.Collections;

public class Button : MonoBehaviour {

    public enum ButtonMode{
        ToggleDoor,
        OpenDoor,
        CloseDoor
    };

    public Door door;
    public ButtonMode buttonMode;

    public Color openColor;
    public Color closedColor;
    public Color errorColor;

    public float blinkingColorReduce = 0.5f;
    public float blinkingDelay = 1f;
    private bool isBlinking = false;
    private float nextBlinkTime = 0f;
    private bool blinkIsOn = false;

    private Material material;

    public MeshRenderer buttonRenderer;
    public Light buttonLight;

    private Door.State doorState;
    private bool first = true;

    public bool debug = false;
    void Start(){

        material = new Material(buttonRenderer.material);
        buttonRenderer.material = material;

        door.AddButton(this);
    }

    void FixedUpdate(){
        if(debug){
            first = true;
            DoorStateChanged(door.state);

        }
        if(!isBlinking) return;

        
        if(nextBlinkTime < Time.time){
            blinkIsOn = !blinkIsOn;

            Color c = (doorState == Door.State.Opening ? openColor : closedColor);

            if(!blinkIsOn)
                c -= new Color(blinkingColorReduce,blinkingColorReduce, blinkingColorReduce, 0);

            setColor(c);

            nextBlinkTime = Time.time + blinkingDelay;
        }
    }

    public void DoorStateChanged(Door.State state){
        if(state != doorState || first){
            first = false;
            switch(state){
                case Door.State.Open:
                    setColor(openColor);
                    isBlinking = false;

                break;
                case Door.State.Closed:
                    setColor(closedColor);
                    isBlinking = false;
                break;

                case Door.State.Opening:
                case Door.State.Closing:
                    isBlinking = true;  
                    nextBlinkTime = Time.time + blinkingDelay;
                    blinkIsOn = false;

                break;
                case Door.State.Error:
                    isBlinking = false;
                    setColor(errorColor);

                break;
            }
            doorState = state;
        }
    }

    private void setColor(Color color){
        material.color = color;
        material.SetColor("_EmissionColor", color);
        buttonLight.color = color;
     
    }

    void OnTriggerEnter2D(Collider2D coll){
        if(coll.gameObject.tag == "Player"){
            switch(buttonMode){
                case ButtonMode.ToggleDoor:
                    door.Toggle();
                    break;
                case ButtonMode.OpenDoor:
                    door.Open();
                    break;
                case ButtonMode.CloseDoor:
                    door.Close();
                    break;
            }
        }
    }
}
