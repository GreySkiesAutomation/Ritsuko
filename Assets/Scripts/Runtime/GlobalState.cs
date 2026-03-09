using System;
using Configuration;
namespace Runtime
{
    [Serializable]
    public class GlobalState
    {
        public BehaviourMode CurrentMode = BehaviourMode.ProductiveWork;
    }
}