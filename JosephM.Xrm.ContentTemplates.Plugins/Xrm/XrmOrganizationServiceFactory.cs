using Microsoft.Xrm.Sdk;
using System;

namespace JosephM.Xrm.ContentTemplates.Plugins.Xrm
{
    public class XrmOrganizationServiceFactory
    {
        private object _lockObject = new Object();

        public IOrganizationService GetOrganisationService(IXrmConfiguration xrmConfiguration)
        {
            if (!xrmConfiguration.UseXrmToolingConnector)
            {
                return XrmConnection.GetOrgServiceProxy(xrmConfiguration);
            }
            else
            {
                throw new NotSupportedException("Tooling Conenction Not Supported In This Project");
            }
        }
    }
}