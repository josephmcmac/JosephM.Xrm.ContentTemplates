using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using System;

namespace JosephM.Xrm.ContentTemplates.Plugins.SharePoint
{
    public class JosephMContentTemplatesSharePointSettings : ISharePointSettings
    {
        public JosephMContentTemplatesSharePointSettings(XrmService xrmService)
        {
            XrmService = xrmService;
        }

        private string _username;
        public string UserName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private string _password;
        public string Password
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private XrmService XrmService { get; }
    }
}
