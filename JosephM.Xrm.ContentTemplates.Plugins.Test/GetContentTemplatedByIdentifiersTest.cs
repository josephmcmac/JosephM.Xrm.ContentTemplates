using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Schema;
using System;
using System.Collections.Generic;

namespace JosephM.Xrm.ContentTemplates.Plugins.Test
{
    [TestClass]
    public class GetContentTemplatedByIdentifiersTests : JosephMContentTemplatesXrmTest
    {
        [TestMethod]
        public void GetContentTemplatedByIdentifiersTest()
        {
            DeleteAllEntityType(Entities.jmcg_contenttemplate);

            var contentTemplate = CreateTestRecord(Entities.jmcg_contenttemplate, new Dictionary<string, object>
            {
                { Fields.jmcg_contenttemplate_.jmcg_name, "Testing Action" },
                { Fields.jmcg_contenttemplate_.jmcg_subject, "Subject [firstname]" },
                { Fields.jmcg_contenttemplate_.jmcg_content, "Body [lastname]" },
            });

            var request = new OrganizationRequest(Actions.jmcg_GetContentTemplatedByIdentifiers.Name);
            request[Actions.jmcg_GetContentTemplatedByIdentifiers.In.ContentTemplateIdentifier] =  "Testing Action";
            request[Actions.jmcg_GetContentTemplatedByIdentifiers.In.TargetId] =  TestContact.Id.ToString();
            request[Actions.jmcg_GetContentTemplatedByIdentifiers.In.TargetType] =  TestContact.LogicalName;

            var response = (OrganizationResponse)XrmService.Execute(request);

            Assert.AreEqual($"Subject {TestContact.GetStringField(Fields.contact_.firstname)}", (string)response[Actions.jmcg_GetContentTemplatedByIdentifiers.Out.TemplateSubjectResult]);
            Assert.AreEqual($"Body {TestContact.GetStringField(Fields.contact_.lastname)}", (string)response[Actions.jmcg_GetContentTemplatedByIdentifiers.Out.TemplateContentResult]);
        }
    }
} 