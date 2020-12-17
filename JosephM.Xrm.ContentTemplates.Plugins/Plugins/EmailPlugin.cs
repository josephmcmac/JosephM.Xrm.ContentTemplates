using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Schema;
using System;

namespace JosephM.Xrm.ContentTemplates.Plugins.Plugins
{
    public class EmailPlugin : JosephMContentTemplatesEntityPluginBase
    {
        public override void GoExtention()
        {
            PopulateFromContentTemplate();
            //call both these because the update one wasn't always getting called
            SetActivityCompleteTriggerForSetState();
            SetActivityCompleteTriggerForUpdate();
        }

        private void PopulateFromContentTemplate()
        {
            if (IsMessage(PluginMessage.Create, PluginMessage.Update) && IsStage(PluginStage.PreOperationEvent))
            {
                if (FieldChanging(Fields.email_.jmcg_populatefromregardingontemplate))
                {
                    var templateId = GetLookupGuid(Fields.email_.jmcg_populatefromregardingontemplate);
                    var regardingId = GetLookupGuid(Fields.email_.regardingobjectid);
                    var regardingType = GetLookupType(Fields.email_.regardingobjectid);
                    if (templateId.HasValue)
                    {
                        if(!regardingId.HasValue)
                        {
                            throw new InvalidPluginExecutionException($"{GetFieldLabel(Fields.email_.regardingobjectid)} is required when {GetFieldLabel(Fields.email_.jmcg_populatefromregardingontemplate)} is set");
                        }

                        var response = JosephMContentTemplatesService.GetContent(templateId.Value, regardingType, regardingId.Value, LocalisationService);
                        if(response.Subject != null)
                        {
                            SetField(Fields.email_.subject, response.Subject);
                        }
                        SetField(Fields.email_.description, response.Content);
                    }
                }
            }
        }

        /// <summary>
        /// when various system generated activities are completed the system needs to update a field
        /// in the regarding record to specify completed
        /// </summary>
        public void SetActivityCompleteTriggerForUpdate()
        {
            if (IsMessage(PluginMessage.Update) && IsStage(PluginStage.PostEvent) && IsMode(PluginMode.Synchronous))
            {
                if (OptionSetChangedTo(Fields.activitypointer_.statecode, OptionSets.Activity.ActivityStatus.Completed))
                {
                    SetActivityCompleteTrigger(GetField);
                }
            }
        }

        /// <summary>
        /// when various system generated activities are completed the system needs to update a field
        /// in the regarding record to specify completed
        /// </summary>
        public void SetActivityCompleteTriggerForSetState()
        {
            if (IsMessage(PluginMessage.SetStateDynamicEntity)
                            && IsStage(PluginStage.PostEvent)
                            && IsMode(PluginMode.Synchronous)
                            && SetStateState == OptionSets.Activity.ActivityStatus.Completed)
            {
                SetActivityCompleteTrigger(XrmService.Retrieve(TargetType, TargetId, new[] { Fields.email_.jmcg_setsentonregardingfield, Fields.email_.regardingobjectid, Fields.email_.statecode }).GetField);
            }
        }

        private void SetActivityCompleteTrigger(Func<string, object> getField)
        {
            if (!string.IsNullOrWhiteSpace((string)getField(Fields.email_.jmcg_setsentonregardingfield)))
            {
                var regardingType = XrmEntity.GetLookupType(getField(Fields.activitypointer_.regardingobjectid));
                var regardingId = XrmEntity.GetLookupGuid(getField(Fields.activitypointer_.regardingobjectid));
                if (regardingId.HasValue)
                {
                    var fieldToSet = (string)getField(Fields.email_.jmcg_setsentonregardingfield);
                    var fieldType = XrmService.GetFieldType(fieldToSet, regardingType);
                    var completionTarget = XrmService.Retrieve(regardingType, regardingId.Value, new[] { fieldToSet });
                    if (fieldType == AttributeTypeCode.Boolean)
                    {
                        if (!completionTarget.GetBoolean(fieldToSet))
                            XrmService.SetField(regardingType, regardingId.Value, fieldToSet, true);
                    }
                    else if (fieldType == AttributeTypeCode.DateTime)
                    {
                        if (!XrmEntity.FieldsEqual(completionTarget.GetField(fieldToSet), LocalisationService.TodayUnspecifiedType))
                            XrmService.SetField(regardingType, regardingId.Value, fieldToSet, LocalisationService.TodayUnspecifiedType);
                    }
                    else
                        throw new NotImplementedException(string.Format("Setting the field type {0} of the field {1} on {2} type is not implemented", fieldType, fieldToSet, regardingType));
                }
            }
        }
    }
}