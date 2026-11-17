// ----------------------------------------------------
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using UnityEngine;

namespace PampelGames.Shared.Tools
{
    /// <summary>
    ///     Can be attached to pooled particle systems to despawn them automatically when all particles have died.
    /// </summary>
    public class PGPoolableParticles : MonoBehaviour
    {
        public ParticleSystem _particleSystem;

        public bool poolActive = true;

        private void Reset()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        private void Start()
        {
            var main = _particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        private void OnParticleSystemStopped()
        {
            if(poolActive) PGPool.Release(gameObject);
            else Destroy(gameObject);
        }
    }
}