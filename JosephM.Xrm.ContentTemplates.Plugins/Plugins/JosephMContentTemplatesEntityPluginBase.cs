using JosephM.Xrm.ContentTemplates.Plugins.Localisation;
using JosephM.Xrm.ContentTemplates.Plugins.Rollups;
using JosephM.Xrm.ContentTemplates.Plugins.Services;
using JosephM.Xrm.ContentTemplates.Plugins.SharePoint;
using JosephM.Xrm.ContentTemplates.Plugins.Xrm;

namespace JosephM.Xrm.ContentTemplates.Plugins.Plugins
{
    /// <summary>
    /// class for shared services or settings objects for plugins
    /// </summary>
    public abstract class JosephMContentTemplatesEntityPluginBase : XrmEntityPlugin
    {
        private JosephMContentTemplatesSettings _settings;
        public JosephMContentTemplatesSettings JosephMContentTemplatesSettings
        {
            get
            {
                if (_settings == null)
                    _settings = new JosephMContentTemplatesSettings(XrmService);
                return _settings;
            }
        }

        private JosephMContentTemplatesService _service;
        public JosephMContentTemplatesService JosephMContentTemplatesService
        {
            get
            {
                if (_service == null)
                    _service = new JosephMContentTemplatesService(XrmService, LocalisationService, JosephMContentTemplatesSettings);
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
                    _localisationService = new LocalisationService(new UserLocalisationSettings(XrmService, Context.InitiatingUserId));
                return _localisationService;
            }
        }
    }
}
