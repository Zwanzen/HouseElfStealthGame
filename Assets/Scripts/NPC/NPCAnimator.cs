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
        
        public enum AnimState
        {
            Idle,
            Walk,
        }
        
        private readonly Dictionary<AnimState, string> _animStateToBool = new Dictionary<AnimState, string>
        {
            { AnimState.Idle, "Idle" },
            { AnimState.Walk, "Walk" },
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

        private float _blinkTimer = Random.Range(1f, 10f);
        private void HandleBlinking(float delta)
        {
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
            CycleEmotion(delta);
        }
        
        private float _emotionTimer;
        private int _emotionIndex;
        
        private void CycleEmotion(float delta)
        {
            _emotionTimer += delta;
            if (_emotionTimer >= 2f)
            {
                _emotionIndex++;
                if(_emotionIndex > 3)
                    _emotionIndex = 0;
                _emotionTimer = 0f;
                _animator.SetInteger(Emotion, _emotionIndex);
            }
        }
    
}