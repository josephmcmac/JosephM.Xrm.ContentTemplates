using JosephM.Xrm.ContentTemplates.Plugins.Xrm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Schema;
using System;
using System.Collections.Generic;

namespace JosephM.Xrm.ContentTemplates.Plugins.Test
{
    [TestClass]
    public class EmailTests : JosephMContentTemplatesXrmTest
    {
        [TestMethod]
        public void EmailTestPopulateFromTemplatedRegardingTest()
        {
            DeleteAllEntityType(Entities.jmcg_contenttemplate);

            var contentTemplate = CreateTestRecord(Entities.jmcg_contenttemplate, new Dictionary<string, object>
            {
                { Fields.jmcg_contenttemplate_.jmcg_subject, "Subject [firstname]" },
                { Fields.jmcg_contenttemplate_.jmcg_content, "Body [lastname]" },
            });

            var email = CreateTestRecord(Entities.email, new Dictionary<string, object>
            {
                { Fields.email_.regardingobjectid, TestContact.ToEntityReference() },
                { Fields.email_.jmcg_populatefromregardingontemplate, contentTemplate.ToEntityReference() }
            });

            Assert.AreEqual($"Subject {TestContact.GetStringField(Fields.contact_.firstname)}", email.GetStringField(Fields.email_.subject));
            Assert.AreEqual($"Body {TestContact.GetStringField(Fields.contact_.lastname)}", email.GetStringField(Fields.email_.description));

            Delete(email);
            Delete(contentTemplate);
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