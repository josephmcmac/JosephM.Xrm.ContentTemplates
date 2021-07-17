using JosephM.Xrm.ContentTemplates.Plugins.Localisation;
using JosephM.Xrm.ContentTemplates.Plugins.Services;
using JosephM.Xrm.ContentTemplates.Plugins.Xrm;

namespace JosephM.Xrm.ContentTemplates.Plugins.Workflows
{
    /// <summary>
    /// class for shared services or settings objects for workflow activities
    /// </summary>
    public abstract class JosephMContentTemplatesWorkflowActivity<T> : XrmWorkflowActivityInstance<T>
        where T : XrmWorkflowActivityRegistration
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

        private LocalisationService _localisationService;
        public LocalisationService LocalisationService
        {
            get
            {
                if (_localisationService == null)
                    _localisationService = new LocalisationService(new UserLocalisationSettings(XrmService, InitiatingUserId));
                return _localisationService;
            }
        }
    }
}
