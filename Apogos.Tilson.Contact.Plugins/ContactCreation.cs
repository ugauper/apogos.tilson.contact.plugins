using Models = Apogos.Dynamics.Common.Models;
using Apogos.Dynamics.Common.Interfaces;
using Apogos.Dynamics.Common.Plugins;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Apogos.Dynamics.Common.Services;

namespace Apogos.Tilson.Contact.Plugins
{
    public class ContactCreation : PluginBase
    {
        private const string _primaryContactIdAttribute = "primarycontactid";
        private const string _parentCustomerIdAttribute = "parentcustomerid";
        private const string _contactTypeAttribute = "apogos_contacttype";
        private const int _primaryContactType = 255290001;
        public override List<Models.PluginMessage> SupportedMessages => new List<Models.PluginMessage> { Models.PluginMessage.Create };

        public override List<string> SupportedEntities => new List<string> { "contact" };

        public override List<Models.PluginStage> SupportedStages => new List<Models.PluginStage> { Models.PluginStage.PostOperation };

        public override void Process()
        {
            var contact = new Models.Contact(TargetEntity);
            var accountReference = contact.GetAttributeValue<EntityReference>(_parentCustomerIdAttribute);

            if (accountReference == null) { return; }

            var contactTypes = contact.GetAttributeValue<OptionSetValueCollection>(_contactTypeAttribute);
            var primaryContactTypeOption = new OptionSetValue(_primaryContactType);

            if (!contactTypes.Contains(primaryContactTypeOption)) { return; }

            var accountService = new AccountService(OrgService, TracingService);
            var accountId = accountReference.Id;

            SetContactAsAccountPrimaryContact(accountService, accountId, contact);
        }

        public void SetContactAsAccountPrimaryContact(IAccountService accountService, Guid accountId, Models.Contact contact)
        {
            var account = (Models.Account)accountService.Get(accountId);
            var existingPrimaryContact = account.GetAttributeValue<EntityReference>(_primaryContactIdAttribute);

            if (existingPrimaryContact != null) return;

            var contactReference = new EntityReference(new Models.Contact().EntityName, contact.Id);
            account.SetAttribute(_primaryContactIdAttribute, contactReference);
            accountService.Update(account);
        }
    }
}
