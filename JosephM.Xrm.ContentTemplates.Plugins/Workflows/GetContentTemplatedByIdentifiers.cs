using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Schema;
using System;
using System.Activities;

namespace JosephM.Xrm.ContentTemplates.Plugins.Workflows
{
    /// <summary>
    /// This class is for the static type required for registration of the custom workflow activity in CRM
    /// </summary>
    public class GetContentTemplatedByIdentifiers : XrmWorkflowActivityRegistration
    {
        [Input("Content Template Name")]
        public InArgument<string> ContentTemplateIdentifier { get; set; }

        [Input("Target Type")]
        public InArgument<string> TargetType { get; set; }

        [Input("Target Id")]
        public InArgument<string> TargetId { get; set; }

        [Output("Template Content Result")]
        public OutArgument<string> TemplateContentResult { get; set; }

        [Output("Template Subject Result")]
        public OutArgument<string> TemplateSubjectResult { get; set; }

        protected override XrmWorkflowActivityInstanceBase CreateInstance()
        {
            return new GetContentTemplatedByIdentifiersInstance();
        }
    }

    /// <summary>
    /// This class is instantiated per execution
    /// </summary>
    public class GetContentTemplatedByIdentifiersInstance
        : JosephMContentTemplatesWorkflowActivity<GetContentTemplatedByIdentifiers>
    {
        protected override void Execute()
        {
            var templateIdentifier = ActivityThisType.ContentTemplateIdentifier.Get(ExecutionContext);
            if (string.IsNullOrWhiteSpace(templateIdentifier))
            {
                throw new InvalidPluginExecutionException($"{nameof(ActivityThisType.ContentTemplateIdentifier)} is a required input argument");
            }
            var contentTemplate = XrmService.GetFirst(Entities.jmcg_contenttemplate, Fields.jmcg_contenttemplate_.jmcg_name, templateIdentifier, new [] { Fields.jmcg_contenttemplate_.jmcg_contenttemplateid });
            if(contentTemplate == null)
            {
                throw new InvalidPluginExecutionException($"Could not find {XrmService.GetEntityDisplayName(Entities.contracttemplate)} with {XrmService.GetFieldLabel(Fields.jmcg_contenttemplate_.jmcg_name, Entities.jmcg_contenttemplate)} = {templateIdentifier}");
            }

            var targetType = ActivityThisType.TargetType.Get(ExecutionContext);
            if (string.IsNullOrWhiteSpace(targetType))
            {
                throw new InvalidPluginExecutionException($"{nameof(ActivityThisType.TargetType)} is a required input argument");
            }

            var targetIdString = ActivityThisType.TargetId.Get(ExecutionContext);
            if (string.IsNullOrWhiteSpace(targetIdString))
            {
                throw new InvalidPluginExecutionException($"{nameof(ActivityThisType.TargetId)} is a required input argument");
            }
            var targetId = Guid.Empty;
            if(!Guid.TryParse(targetIdString, out targetId))
            {
                throw new InvalidPluginExecutionException($"Input argument {nameof(ActivityThisType.TargetId)} is required to be a valid {typeof(Guid).Name} value");
            }

            var templateResponse = JosephMContentTemplatesService.GenerateForContentTemplate(contentTemplate.Id, targetType, targetId, LocalisationService);

            ActivityThisType.TemplateSubjectResult.Set(ExecutionContext, templateResponse.Subject ?? "");
            ActivityThisType.TemplateContentResult.Set(ExecutionContext, templateResponse.Content ?? "");
        }

    }
}
