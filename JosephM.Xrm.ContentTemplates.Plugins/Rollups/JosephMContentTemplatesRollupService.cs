using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.Xrm.Sdk;
using Schema;
using System;
using System.Collections.Generic;

namespace JosephM.Xrm.ContentTemplates.Plugins.Rollups
{
    public class JosephMContentTemplatesRollupService : RollupService
    {
        public JosephMContentTemplatesRollupService(XrmService xrmService)
            : base(xrmService)
        {
        }

        private IEnumerable<LookupRollup> _Rollups = new LookupRollup[]
        {
        };

        public override IEnumerable<LookupRollup> AllRollups => _Rollups;
    }
}