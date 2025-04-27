using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator
{
    // Components
    private PlayerController _player;
    private Animator _anim;

    // Constructor
    public PlayerAnimator(PlayerController player, Animator anim)
    {
        _player = player;
        _anim = anim;
    }

    // Properties
    public AnimatorStateInfo CurrentAnimInfo => GetCurrentAnimInfo();


    // Private Methods
    private AnimatorStateInfo GetCurrentAnimInfo()
    {
        return _anim.GetCurrentAnimatorStateInfo(0);
    }

    // This gets converts the defined animation type to a hash value
    private int GetAnimHash(EAnimType animType)
    {
        if (_animNames.TryGetValue(animType, out string animName))
            return Animator.StringToHash(animName);
        else
            Debug.LogError($"Animation type {animType} not found in dictionary.");
        return -1;
    }

    // This method gets the animation name from the enum type
    private string GetAnimName(EAnimType animType)
    {
        if (_animNames.TryGetValue(animType, out string animName))
            return animName;
        else
            Debug.LogError($"Animation type {animType} not found in dictionary.");
        return string.Empty;
    }

    // This is used to define what types of animations are available for the player
    private Dictionary<EAnimType, string> _animNames = new Dictionary<EAnimType, string>()
    {
        { EAnimType.Stealth, "Stealth" },
        { EAnimType.Fall, "Fall" },
        { EAnimType.FallHit, "FallHit" },
        { EAnimType.FallStop, "FallStop" },
        { EAnimType.FallStand, "FallStand" },
    };

    // This is used to define what anim type is a bool or trigger
    private Dictionary<EAnimType, bool> _isTigger = new Dictionary<EAnimType, bool>()
    {
        { EAnimType.Stealth, false },
        { EAnimType.Fall, true },
        { EAnimType.FallHit, true },
        { EAnimType.FallStop, false },
        { EAnimType.FallStand, true },
    };

    // Public Methods
    public enum EAnimType
    {
        Stealth,
        Fall,
        FallHit,
        FallStop,
        FallStand,
    }

    /// <summary>
    /// This method is used to play an animation by animation type.
    /// It checks if the animation type is a trigger or a bool.
    /// </summary>
    public void SetAnim(EAnimType animType)
    {
        int animHash = GetAnimHash(animType);
        if (animHash != -1)
            if (_isTigger.TryGetValue(animType, out bool isTrigger))
                if (isTrigger)
                {
                    // We need to check if this trigger animation is already playing
                    // If it is, we don't want to set it again
                    if (_anim.GetCurrentAnimatorStateInfo(0).IsName(GetAnimName(animType)))
                        return;
                    _anim.SetTrigger(animHash);
                }
                else
                    _anim.SetBool(animHash, true);
            else
                Debug.LogError($"Animation type {animType} not found in dictionary.");
    }

    /// <summary>
    /// This method is used to stop an animation by animation type.
    /// </summary>
    public void OffAnim(EAnimType animType)
    {
        int animHash = GetAnimHash(animType);
        if (animHash != -1)
            if (_isTigger.TryGetValue(animType, out bool isTrigger))
                if (!isTrigger)
                    _anim.SetBool(animHash, false);
                else
                    _anim.ResetTrigger(animHash);
    }

    /// <summary>
    /// This method is used to force an animation to play.
    /// </summary>
    public void ForceAnim(EAnimType animType)
    {
        int animHash = GetAnimHash(animType);
        if (animHash != -1)
            _anim.Play(animHash, 0, 0f);
        else
            Debug.LogError($"Animation type {animType} not found in dictionary.");
    }

    public void ResetTriggers()
    {
        foreach (var pair in _isTigger)
        {
            if (pair.Value)
                _anim.ResetTrigger(GetAnimHash(pair.Key));
        }
    }

    // Created Using Claude/Copilot
    #region Animation Callbacks

    // Animation state tracking
    private EAnimType? _currentAnimationType = null;
    private Dictionary<EAnimType, List<Action>> _animCompletionCallbacks = new Dictionary<EAnimType, List<Action>>();

    // Track animation time completion
    private float _prevNormalizedTime = 0f;
    private bool _currentAnimationIsLooping = false;

    /// <summary>
    /// Register a callback that will be triggered when a specific animation completes.
    /// The callback will automatically be deregistered after being called once.
    /// </summary>
    public void RegisterAnimationCallback(EAnimType animType, Action callback)
    {
        if (callback == null)
            return;

        if (!_animCompletionCallbacks.ContainsKey(animType))
            _animCompletionCallbacks[animType] = new List<Action>();

        _animCompletionCallbacks[animType].Add(callback);
    }

    /// <summary>
    /// Unregister a completion callback for an animation type
    /// </summary>
    public void UnregisterAnimationCompleteCallback(EAnimType animType, Action callback = null)
    {
        if (!_animCompletionCallbacks.ContainsKey(animType))
            return;

        if (callback == null)
            _animCompletionCallbacks.Remove(animType);
        else
            _animCompletionCallbacks[animType].Remove(callback);
    }

    /// <summary>
    /// This method should be called from PlayerController's Update method
    /// </summary>
    public void Update()
    {
        HandleRegiseredCallbacks();
    }

    /// <summary>
    /// This method handles registered callbacks for animation completion.
    /// This needs to be called every frame to check if the animation has completed or looped.
    /// </summary>
    private void HandleRegiseredCallbacks()
    {
        AnimatorStateInfo currentState = CurrentAnimInfo;

        // Find which animation is currently playing
        EAnimType? newAnimationType = null;
        foreach (var pair in _animNames)
        {
            if (currentState.IsName(pair.Value))
            {
                newAnimationType = pair.Key;
                break;
            }
        }

        // Case 1: Animation has changed - the previous animation has completed or was interrupted
        if (_currentAnimationType.HasValue && _currentAnimationType != newAnimationType)
        {
            TriggerCallbacks(_currentAnimationType.Value);

            // Reset time tracking for the new animation
            _prevNormalizedTime = 0f;
            _currentAnimationIsLooping = newAnimationType.HasValue ? currentState.loop : false;
        }
        // Case 2: Same animation is still playing - check if it completed a cycle (for non-looping animations)
        else if (_currentAnimationType.HasValue && _currentAnimationType == newAnimationType)
        {
            // Get normalized time (0 to 1 range, or higher for multiple loops)
            float normalizedTime = currentState.normalizedTime;

            // For non-looping animations, check if we've reached the end
            if (!_currentAnimationIsLooping &&
                _prevNormalizedTime < 0.95f && normalizedTime >= 0.95f)
            {
                TriggerCallbacks(_currentAnimationType.Value);
            }
            // For looping animations, check if we've completed a loop
            else if (_currentAnimationIsLooping &&
                     _prevNormalizedTime > normalizedTime &&
                     _prevNormalizedTime > 0.95f && normalizedTime < 0.05f)
            {
                TriggerCallbacks(_currentAnimationType.Value);
            }

            // Update previous time
            _prevNormalizedTime = normalizedTime;
        }

        // Update the current animation
        if (_currentAnimationType != newAnimationType)
        {
            _currentAnimationType = newAnimationType;
            if (newAnimationType.HasValue)
            {
                _currentAnimationIsLooping = currentState.loop;
                _prevNormalizedTime = currentState.normalizedTime;
            }
        }
    }

    // Helper to trigger callbacks and clear them
    private void TriggerCallbacks(EAnimType animType)
    {
        if (_animCompletionCallbacks.TryGetValue(animType, out var callbacks) && callbacks.Count > 0)
        {
            // Copy the callbacks to avoid modification issues during iteration
            var callbacksCopy = new List<Action>(callbacks);

            // Clear all callbacks for this animation as they're designed to run once
            callbacks.Clear();

            // Execute all the callbacks
            foreach (var callback in callbacksCopy)
            {
                callback?.Invoke();
            }
        }
    }
    #endregion
}
