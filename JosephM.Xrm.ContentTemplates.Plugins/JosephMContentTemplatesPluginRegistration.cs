using JosephM.Xrm.ContentTemplates.Plugins.Plugins;
using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Schema;

namespace JosephM.Xrm.ContentTemplates.Plugins
{
    /// <summary>
    /// This is the class for registering plugins in CRM
    /// Each entity plugin type needs to be instantiated in the CreateEntityPlugin method
    /// </summary>
    public class JosephMContentTemplatesPluginRegistration : XrmPluginRegistration
    {
        public override XrmPlugin CreateEntityPlugin(string entityType, bool isRelationship)
        {
            switch (entityType)
            {
                case Entities.email: return new EmailPlugin();
            }
            return null;
        }
    }
}
