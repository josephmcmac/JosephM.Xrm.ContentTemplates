using JosephM.Xrm.ContentTemplates.Plugins.Localisation;
using JosephM.Xrm.ContentTemplates.Plugins.Rollups;
using JosephM.Xrm.ContentTemplates.Plugins.Services;
using JosephM.Xrm.ContentTemplates.Plugins.SharePoint;
using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.Xrm.Sdk;
using System;

namespace JosephM.Xrm.ContentTemplates.Plugins.Plugins
{
    /// <summary>
    /// class for shared services or settings objects for plugins
    /// </summary>
    public abstract class JosephMContentTemplatesEntityPluginBase : XrmEntityPlugin
    {
        private JosephMContentTemplatesService _service;
        public JosephMContentTemplatesService JosephMContentTemplatesService
        {
            get
            {
                if (_service == null)
                    _service = new JosephMContentTemplatesService(XrmService, LocalisationService);
                return _service;
            }
        }

        private JosephMContentTemplatesRollupService _RollupService;
        public JosephMContentTemplatesRollupService JosephMContentTemplatesRollupService
        {
            get
            {
                if (_RollupService == null)
                    _RollupService = new JosephMContentTemplatesRollupService(XrmService);
                return _RollupService;
            }
        }

        private JosephMContentTemplatesSharepointService _sharePointService;
        public JosephMContentTemplatesSharepointService JosephMContentTemplatesSharepointService
        {
            get
            {
                if (_sharePointService == null)
                    _sharePointService = new JosephMContentTemplatesSharepointService(XrmService, new JosephMContentTemplatesSharePointSettings(XrmService));
                return _sharePointService;
            }
        }

        private LocalisationService _localisationService;
        public LocalisationService LocalisationService
        {
            get
            {
                if (_localisationService == null)
                {
                    Guid? userId = null;
                    if (IsMessage(PluginMessage.Create))
                    {
                        userId = GetLookupGuid("createdonbehalfby");
                        if(!userId.HasValue)
                        {
                            userId = GetLookupGuid("createdby");
                        }
                    }
                    else if (IsMessage(PluginMessage.Update))
                    {
                        userId = GetLookupGuid("modifiedby");
                    }
                    if (!userId.HasValue)
                    {
                        userId = Context.InitiatingUserId;
                    }
                    _localisationService = new LocalisationService(new UserLocalisationSettings(XrmService, userId.Value));
                }
                return _localisationService;
            }
        }
    }
}
