﻿using JosephM.Xrm.ContentTemplates.Plugins.Core;
using Microsoft.Xrm.Sdk;

namespace JosephM.Xrm.ContentTemplates.Plugins.Xrm
{
    public class XrmTraceUserInterface : IUserInterface
    {
        private readonly ITracingService _trace;

        public XrmTraceUserInterface(ITracingService tracingService)
        {
            _trace = tracingService;
            UiActive = true;
        }

        public void LogDetail(string stage)
        {
            //Output(stage);
        }

        public void UpdateProgress(int countCompleted, int countOutOf, string message)
        {
        }

        public bool UiActive { get; set; }

        public void LogMessage(string message)
        {
            _trace.Trace(message, new object[0]);
        }

        private void Output(string literal)
        {
            _trace.Trace(literal, new object[0]);
        }
    }
}