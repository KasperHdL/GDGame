﻿using UnityEngine;
using System.Collections.Generic;

public class Door : MonoBehaviour {

    public enum State{
        Closed,
        Closing,
        Open,
        Opening,
        Error,
    };

    public State state;

    public float openDuration;
    private float aniTime;

    public float minScale;
    private float maxScale;

    private Vector3 startPosition;
    private float moveAmount;

    public Vector3 openDirection;
    public AnimationCurve doorAnimationCurve;

    public List<Button> buttons;

    void OnReset(){
        if(openDirection == Vector3.zero)
            openDirection = transform.up;
    }

	// Use this for initialization
	void Awake () {
        maxScale = transform.localScale.y;
        moveAmount = maxScale / 2;
        
        if(openDirection == Vector3.zero)
            openDirection = transform.up;

        startPosition = transform.position + openDirection * moveAmount;

        if(state == State.Open || state == State.Opening)
            aniTime = 0;
        else
            aniTime = openDuration;

        SetDoor();
	}

    public void AddButton(Button b){
        buttons.Add(b);
        b.DoorStateChanged(state);
    }

    public void RemoveButton(Button b){
        buttons.Remove(b);
    }

    private void ChangeState(State state){
        this.state = state;
        for(int i = 0; i < buttons.Count; i++){
            buttons[i].DoorStateChanged(state);
        }
    }
	
	// Update is called once per frame
	void FixedUpdate () {
        if(state == State.Closed || state == State.Open)
            return;

        SetDoor();

        switch(state){
            case State.Opening:
                aniTime -= Time.fixedDeltaTime;
                if(aniTime <= 0){
                    aniTime = 0;
                    ChangeState(State.Open);
                }
            break;
            case State.Closing:
                aniTime += Time.fixedDeltaTime;
                if(aniTime >= openDuration){
                    aniTime = openDuration;
                    ChangeState(State.Closed);
                }
            break;
        }
	}
    
    private void SetDoor(){
        float t = doorAnimationCurve.Evaluate(aniTime / openDuration);
        transform.position = startPosition - openDirection * moveAmount * t;

        Vector3 s = transform.localScale;
        s.y = (maxScale - minScale) * t;
        transform.localScale = s;
    }

    public void Toggle()
    {

        if (state == State.Closed)
            Open();
        else if (state == State.Open)
            Close();
        else if(state == State.Error)
            Error();
    }

    public void Open(){
        if(state == State.Error){
            Error();
        }
        else if (state != State.Open)
        {
            ChangeState(State.Opening);
            //todo: make relative to player location
            SoundSystem.Play("doorOpen");
        }
    }

    public void Close(){
        if(state == State.Error){
            Error();
        }
        else if (state != State.Closed)
        {
            ChangeState(State.Closing);
            //todo: make relative to player location
            SoundSystem.Play("doorClose");

        }
    }

    public void Error(){
        ChangeState(State.Error);
    }



}
