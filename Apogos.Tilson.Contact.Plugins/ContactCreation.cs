using Models = Apogos.Dynamics.Common.Models;
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
        private const string _primaryContactIdAttribute = "primarycontactid";
        private const string _primaryContactIdValueAttribute = "_primarycontactid_value";
        private const string _parentCustomerIdAttribute = "parentcustomerid";
        private const string _contactTypeAttribute = "apogos_contacttype";
        public override List<Models.PluginMessage> SupportedMessages => new List<Models.PluginMessage> { Models.PluginMessage.Create };

        public override List<string> SupportedEntities => new List<string> { "contact" };

        public override List<Models.PluginStage> SupportedStages => new List<Models.PluginStage> { Models.PluginStage.PostOperation };

        public override void Process()
        {
            var contact = new Models.Contact(TargetEntity);
            var accountId = contact.GetAttributeValue<Guid>(_parentCustomerIdAttribute);

            var contactReference = new EntityReference(new Models.Contact().EntityName, contact.Id);
            account.SetAttribute(_primaryContactIdAttribute, contactReference);
            _accountService.Update(account);
        }

        public Models.Account SetContactAsAccountPrimaryContact(Models.Contact contact)
        {
            return new Models.Account();
        }
    }
}
