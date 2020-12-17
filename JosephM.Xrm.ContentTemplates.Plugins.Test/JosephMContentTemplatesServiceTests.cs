using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Schema;
using System;
using System.Collections.Generic;

namespace JosephM.Xrm.ContentTemplates.Plugins.Test
{
    [TestClass]
    public class JosephMContentTemplatesServiceTests : JosephMContentTemplatesXrmTest
    {
        [TestMethod]
        public void ServiceContentGenerationTest()
        {
            DeleteAllEntityType(Entities.jmcg_contenttemplate);

            var contentTemplate = CreateTestRecord(Entities.jmcg_contenttemplate, new Dictionary<string, object>
            {
                { Fields.jmcg_contenttemplate_.jmcg_subject, "Subject [title]" },
                { Fields.jmcg_contenttemplate_.jmcg_content, "First Name [customerid|contact.firstname] Description [if|description][description][endif] Created By [createdby.fullname] Today [TODAY] Created On [createdon] Follow Up By [followupby]" },
            });

            var aCase = CreateTestRecord(Entities.incident, new Dictionary<string, object>
            {
                { Fields.incident_.title, "Test Case" },
                { Fields.incident_.customerid, TestContact.ToEntityReference() },
                { Fields.incident_.followupby, DateTime.Now.AddDays(10) }
            });

            var contentResponse = JosephMContentTemplatesService.GetContent(contentTemplate.Id, aCase.LogicalName, aCase.Id, LocalisationService);
            Assert.AreEqual($"Subject {aCase.GetStringField(Fields.incident_.title)}", contentResponse.Subject);
            var LocalCreatedOn = LocalisationService.ConvertToTargetTime(aCase.GetDateTimeField(Fields.incident_.createdon).Value);
            var localFollowUpBy = LocalisationService.ConvertToTargetTime(aCase.GetDateTimeField(Fields.incident_.followupby).Value);
            Assert.AreEqual($"First Name {TestContact.GetStringField(Fields.contact_.firstname)} Description  Created By {XrmService.LookupField(Entities.systemuser, CurrentUserId, Fields.systemuser_.fullname)} Today {LocalisationService.ToDateDisplayString(DateTime.Today)} Created On {LocalisationService.ToDateTimeDisplayString(LocalCreatedOn)} Follow Up By {LocalisationService.ToDateDisplayString(localFollowUpBy)}", contentResponse.Content);


            aCase = CreateTestRecord(Entities.incident, new Dictionary<string, object>
            {
                { Fields.incident_.title, "Test Case" },
                { Fields.incident_.customerid, TestContact.ToEntityReference() },
                { Fields.incident_.description, "Test Description" },
            });

            contentResponse = JosephMContentTemplatesService.GetContent(contentTemplate.Id, aCase.LogicalName, aCase.Id, LocalisationService);
            Assert.AreEqual($"Subject {aCase.GetStringField(Fields.incident_.title)}", contentResponse.Subject);
            LocalCreatedOn = LocalisationService.ConvertToTargetTime(aCase.GetDateTimeField(Fields.incident_.createdon).Value);
            Assert.AreEqual($"First Name {TestContact.GetStringField(Fields.contact_.firstname)} Description {aCase.GetStringField(Fields.incident_.description)} Created By {XrmService.LookupField(Entities.systemuser, CurrentUserId, Fields.systemuser_.fullname)} Today {LocalisationService.ToDateDisplayString(DateTime.Today)} Created On {LocalisationService.ToDateTimeDisplayString(LocalCreatedOn)} Follow Up By ", contentResponse.Content);

            DeleteMyToday();
        }

        [TestMethod]
        public void EmailSetSentFieldOnRegardingTest()
        {
            if (TestContact.GetBoolean(Fields.contact_.isbackofficecustomer))
            {
                TestContact.SetField(Fields.contact_.isbackofficecustomer, false);
                TestContact = UpdateFieldsAndRetreive(TestContact, Fields.contact_.isbackofficecustomer);
            }

            var email = CreateTestRecord(Entities.email, new Dictionary<string, object>
            {
                { Fields.email_.regardingobjectid, TestContact.ToEntityReference() },
                { Fields.email_.subject, "Testing Sent " + DateTime.Now.ToFileTime().ToString() },
                { Fields.email_.jmcg_setsentonregardingfield, Fields.contact_.isbackofficecustomer }
            });
            email.AddActivityParty(Fields.email_.from, Entities.systemuser, CurrentUserId);
            email.AddActivityParty(Fields.email_.to, TestContact.LogicalName, TestContact.Id);
            email = UpdateFieldsAndRetreive(email, Fields.email_.from, Fields.email_.to);

            XrmService.SendEmail(email.Id);

            TestContact = Refresh(TestContact);
            Assert.IsTrue(TestContact.GetBoolean(Fields.contact_.isbackofficecustomer));

            if (TestContact.GetField(Fields.contact_.lastusedincampaign) != null)
            {
                TestContact.SetField(Fields.contact_.lastusedincampaign, null);
                TestContact = UpdateFieldsAndRetreive(TestContact, Fields.contact_.lastusedincampaign);
            }

            email = CreateTestRecord(Entities.email, new Dictionary<string, object>
            {
                { Fields.email_.regardingobjectid, TestContact.ToEntityReference() },
                { Fields.email_.subject, "Testing Sent " + DateTime.Now.ToFileTime().ToString() },
                { Fields.email_.jmcg_setsentonregardingfield, Fields.contact_.lastusedincampaign }
            });
            email.AddActivityParty(Fields.email_.from, Entities.systemuser, CurrentUserId);
            email.AddActivityParty(Fields.email_.to, TestContact.LogicalName, TestContact.Id);
            email = UpdateFieldsAndRetreive(email, Fields.email_.from, Fields.email_.to);

            XrmService.SendEmail(email.Id);

            TestContact = Refresh(TestContact);
            Assert.IsNotNull(TestContact.GetField(Fields.contact_.lastusedincampaign));

            DeleteMyToday();
        }
    }
}