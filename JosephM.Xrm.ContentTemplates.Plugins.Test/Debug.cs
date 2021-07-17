using Microsoft.VisualStudio.TestTools.UnitTesting;
using Schema;

namespace JosephM.Xrm.ContentTemplates.Plugins.Test
{
    //this class just for general debug purposes
    [TestClass]
    public class DebugTests : JosephMContentTemplatesXrmTest
    {
        [TestMethod]
        public void Debug()
        {
            var me = XrmService.WhoAmI();

            var fetch = @"[forfetch|<fetch distinct='true' useraworderby='false' no-lock='false' mapping='logical'>
  <entity name='did_review_detail'>
    <attribute name='did_review_detailid' />
    <filter type='and'>
      <condition attribute='did_templateuseonly' operator='eq' value='1' />
      <condition attribute='statecode' operator='eq' value='0' />
    </filter>
    <link-entity name='did_review_template' to='did_review_detail_review_template_id' from='did_review_templateid' link-type='inner'>
      <filter type='and'>
        <condition attribute='statecode' operator='eq' value='0' />
      </filter>
      <link-entity name='did_jobprofile_did_review_template' to='did_review_templateid' from='did_review_templateid' link-type='inner'>
        <link-entity name='did_jobprofile' to='did_jobprofileid' from='did_jobprofileid' link-type='inner'>
          <filter type='and'>
            <condition attribute='did_jobprofileid' operator='eq' value='[accountid]' />
          </filter>
        </link-entity>
      </link-entity>
    </link-entity>
  </entity>
</fetch>]
    <tr>
        <td style='background-color: #f2f2f2; font-weight: bold'>[did_review_detail_skill_id]</td>
        <td>
            [did_review_detail_skill_id.did_description]
        </td>
    </tr>
    [endforfetch]";

            JosephMContentTemplatesService.PopulateTemplateContent(Entities.account, new[] { TestContactAccount.Id }, LocalisationService, null, fetch, null);
            
        }
    }
}