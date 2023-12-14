using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Services
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private readonly List<Action> _actions = new();
        private bool _queued;

        private void Update()
        {
            if (_queued)
            {
                Action[] actions = null;

                lock (_actions)
                {
                    actions = _actions.ToArray();
                    _actions.Clear();
                    _queued = false;
                }

                foreach (Action action in actions)
                {
                    action();
                }
            }
        }

        public void Dispatch(Action action)
        {
            lock (_actions)
            {
                _actions.Add(action);
                _queued = true;
            }
        }
    }
}