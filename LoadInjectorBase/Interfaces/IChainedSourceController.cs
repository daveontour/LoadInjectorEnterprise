using LoadInjector.RunTime;
using System;
using System.Collections.Generic;

namespace LoadInjectorBase.Interfaces
{
    public interface IChainedSourceController
    {
        void ParentFired(Tuple<Dictionary<string, string>> data);
    }
}