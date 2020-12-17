using JosephM.Xrm.ContentTemplates.Plugins.Services;
using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.Xrm.Sdk;
using Schema;
using System;

namespace JosephM.Xrm.ContentTemplates.Plugins
{
    /// <summary>
    /// A settings object which loads the first record of a given type for accessing its fields/properties
    /// </summary>
    public class JosephMContentTemplatesSettings
    {
        private XrmService XrmService { get; set; }

        public JosephMContentTemplatesSettings(XrmService xrmService)
        {
            XrmService = xrmService;
        }

        private const string EntityType = Entities.organization;

        private Entity _settings;

        public Entity Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = XrmService.GetFirst(EntityType);
                    if (_settings == null)
                        throw new NullReferenceException($"Error getting the {XrmService.GetEntityDisplayName(EntityType)} record. It does not exist or you do not have permissions to view it");
                }
                return _settings;
            }
            set { _settings = value; }
        }

        public string OrganisationName
        {
            get
            {
                return Settings.GetStringField(Fields.organization_.name);
            }
        }
    }
}