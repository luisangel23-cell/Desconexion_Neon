// ----------------------------------------------------
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using UnityEngine;

namespace PampelGames.Shared.Tools
{
    public class PGSchedulerDescr
    {
        internal Coroutine _coroutine;
        internal bool isFrameScheduler;
        internal float currentTime;
        internal bool active;
        internal bool stopped;
        internal float duration;
        internal bool completed;
        internal YieldInstruction yieldInstruction;
        
        internal readonly InternalEvents internalEvents = new();

        internal class InternalEvents
        {
            public Action onUpdate;
            public Action onComplete;
            public Action onPause;
            public Action onResume;
            public Action onStop;
        }
        
    }
}
