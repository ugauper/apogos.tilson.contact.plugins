using Apogos.Dynamics.Common.Models;
using Apogos.Dynamics.Common.Plugins;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apogos.Tilson.Contact.Plugins
{
    public class ContactCreation : PluginBase
    {
        public override List<PluginMessage> SupportedMessages => new List<PluginMessage> { PluginMessage.Create };

        public override List<string> SupportedEntities => new List<string> { "contact" };

        public override List<PluginStage> SupportedStages => new List<PluginStage> { PluginStage.PostOperation };

        public override void Process()
        {
            var contact = new Apogos.Dynamics.Common.Models.Contact(TargetEntity);

        }

        public Account SetContactAsAccountPrimaryContact(Apogos.Dynamics.Common.Models.Contact contact)
        {
            return new Account();
        }
    }
}
