using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        }
    }
}