﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors
{
    public interface IQueueManagerActor : IActor
    {
        Task StartMonitoringAsync();
        Task<string> GetPathAsync();
        Task<bool> IsInitializedAsync();
        Task StopMonitoringAsync();
    }
    public interface ITopicManagerActor : IActor
    {
        Task<bool> IsInitializedAsync();
        Task StartMonitoringAsync();
    }
}
