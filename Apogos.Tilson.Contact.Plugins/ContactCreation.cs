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
        private const string _contactTypePortalAttribute = "apogos_contacttypeportal";
        private const int _primaryContactType = 255290001;
        public override List<Models.PluginMessage> SupportedMessages => new List<Models.PluginMessage> { Models.PluginMessage.Create };

        public override List<string> SupportedEntities => new List<string> { "contact" };

        public override List<Models.PluginStage> SupportedStages => new List<Models.PluginStage> { Models.PluginStage.PostOperation };

        public override void Process()
        {
            var contact = new Models.Contact(TargetEntity);

            var accountService = new AccountService(OrgService, TracingService);
            SetContactAsAccountPrimaryContact(accountService, contact);
        }

        public void SetContactAsAccountPrimaryContact(IAccountService accountService, Models.Contact contact)
        {
            var accountReference = contact.GetAttributeValue<EntityReference>(_parentCustomerIdAttribute);

            if (accountReference == null) { return; }

            var contactTypes = contact.GetAttributeValue<OptionSetValueCollection>(_contactTypeAttribute);
            var accountId = accountReference.Id;

            if (contactTypes != null)
            {
                var primaryContactTypeOption = new OptionSetValue(_primaryContactType);

                if (!contactTypes.Any(c => c.Value == primaryContactTypeOption.Value)) { return; }
            } else
            {
                // This Contact was created through a portal
                var contactTypePortal = contact.GetAttributeValue<string>(_contactTypePortalAttribute);

                if (!contactTypePortal.Contains(_primaryContactType.ToString())) {
                    return;
                }

                var contactTypeOptionSetValues = contactTypePortal.Split(',').Select(contactType => new OptionSetValue(int.Parse(contactType)));
                var contactTypeValueCollection = new OptionSetValueCollection();
                contactTypeValueCollection.AddRange(contactTypeOptionSetValues);

                var partialContact = new Models.Contact(new Entity(new Models.Contact().EntityName, contact.Id));
                partialContact.SetAttribute(_contactTypeAttribute, contactTypeValueCollection);

                partialContact.SetAttribute(_parentCustomerIdAttribute, new EntityReference(new Models.Account().EntityName, accountId));

                var contactService = new ContactService(OrgService, TracingService);
                contactService.Update(partialContact);
            }

            var partialAccount = new Models.Account(new Entity(new Models.Account().EntityName, accountId));

            var account = (Models.Account)accountService.Get(accountId);
           
            var existingPrimaryContact = account.GetAttributeValue<EntityReference>(_primaryContactIdAttribute);

            if (existingPrimaryContact != null) return;

            var contactReference = new EntityReference(new Models.Contact().EntityName, contact.Id);
           
            partialAccount.SetAttribute(_primaryContactIdAttribute, contactReference);
            accountService.Update(partialAccount);
        }
    }
}
