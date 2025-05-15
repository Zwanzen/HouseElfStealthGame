using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NPCAnimator
{
    NPC _npc;
    Animator _animator;

    // Animation Hashes
    private static readonly int Blink = Animator.StringToHash("Blink");
    private static readonly int Emotion = Animator.StringToHash("Emotion");

    public NPCAnimator(NPC npc, Animator animator)
    {
        _npc = npc;
        _animator = animator;
    }

    public enum EmotionType
    {
        Normal,
        Curious,
        Alert,
    }

    public enum AnimState
    {
        Idle,
        Walk,
        WalkAlert,
        Sleep,
        SleepAlert
    }

    private readonly Dictionary<AnimState, string> _animStateToBool = new Dictionary<AnimState, string>
        {
            { AnimState.Idle, "Idle" },
            { AnimState.Walk, "Walk" },
            { AnimState.WalkAlert, "WalkAlert" },
            { AnimState.Sleep, "Sleep" },
            { AnimState.SleepAlert, "SleepAlert" }
        };

    // Used to set up new state
    private void ClearBooleans()
    {
        for (int i = 0; i < _animStateToBool.Count; i++)
            _animator.SetBool(_animStateToBool[(AnimState)i], false);
    }

    public void SetNewAnimState(AnimState state)
    {
        ClearBooleans();
        _animator.SetBool(_animStateToBool[state], true);
    }

    public void SetAnimStateForced(AnimState state)
    {
        // Clear all booleans
        ClearBooleans();
        // Set the new state directly
        _animator.Play(_animStateToBool[state]);
        _animator.SetBool(_animStateToBool[state], true);
    }

    private float _blinkTimer = Random.Range(1f, 10f);
    private void HandleBlinking(float delta)
    {
        // If the NPC is sleeping, don't blink
        if (_npc.Type == NPC.NPCType.Sleep)
            return;

        // Handle blinking
        _blinkTimer -= delta;
        if (_blinkTimer <= 0)
        {
            _animator.SetTrigger(Blink);
            _blinkTimer = Random.Range(1f, 10f);
        }
    }

    public void Update(float delta)
    {
        HandleBlinking(delta);
    }



    public void SetEmotion(EmotionType emotion)
    {
        int emotionIndex = 0;
        // Based on the emotion type, set the index
        switch (emotion)
        {
            case EmotionType.Normal:
                emotionIndex = 0;
                break;
            case EmotionType.Curious:
                emotionIndex = 1;
                break;
            case EmotionType.Alert:
                emotionIndex = 2;
                break;
        }

        _animator.SetInteger(Emotion, emotionIndex);
    }

}