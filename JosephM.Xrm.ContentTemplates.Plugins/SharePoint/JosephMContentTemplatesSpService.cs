using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using System.Collections.Generic;

namespace JosephM.Xrm.ContentTemplates.Plugins.SharePoint
{
    public class JosephMContentTemplatesSharepointService : SharePointService
    {
        public JosephMContentTemplatesSharepointService(XrmService xrmService, ISharePointSettings sharepointSettings)
            : base(sharepointSettings, xrmService)
        {
        }

        public override IEnumerable<SharepointFolderConfig> SharepointFolderConfigs
        {
            get
            {

                return new SharepointFolderConfig[]
                {
                };
            }
        }
    }
}
