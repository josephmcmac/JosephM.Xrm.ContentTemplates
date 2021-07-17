using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Schema;
using System.Activities;

namespace JosephM.Xrm.ContentTemplates.Plugins.Workflows
{
    /// <summary>
    /// This class is for the static type required for registration of the custom workflow activity in CRM
    /// </summary>
    public class GetContentTemplatedOnTarget : XrmWorkflowActivityRegistration
    {
        [Input("Content Template")]
        [ReferenceTarget(Entities.jmcg_contenttemplate)]
        public InArgument<EntityReference> ContentTemplate { get; set; }

        [Output("Template Content Result")]
        public OutArgument<string> TemplateContentResult { get; set; }

        [Output("Template Subject Result")]
        public OutArgument<string> TemplateSubjectResult { get; set; }

        protected override XrmWorkflowActivityInstanceBase CreateInstance()
        {
            return new GetContentTemplatedOnTargetInstance();
        }
    }

    /// <summary>
    /// This class is instantiated per execution
    /// </summary>
    public class GetContentTemplatedOnTargetInstance
        : JosephMContentTemplatesWorkflowActivity<GetContentTemplatedOnTarget>
    {
        protected override void Execute()
        {
            var templateReference = ActivityThisType.ContentTemplate.Get(ExecutionContext);
            if (templateReference == null)
            {
                throw new InvalidPluginExecutionException($"{nameof(ActivityThisType.ContentTemplate)} is a required input argument");
            }

            var templateResponse = JosephMContentTemplatesService.GenerateForContentTemplate(templateReference.Id, TargetType, TargetId, LocalisationService);

            ActivityThisType.TemplateSubjectResult.Set(ExecutionContext, templateResponse.Subject);
            ActivityThisType.TemplateContentResult.Set(ExecutionContext, templateResponse.Content);
        }

    }
}
