using Apogos.Dynamics.Common.Configuration;
using models = Apogos.Dynamics.Common.Models;
using Apogos.Dynamics.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Xrm.Sdk;
using System;
using System.IO;
using Xunit;
using Apogos.Dynamics.Common.Interfaces;

namespace Apogos.Tilson.Contact.Plugins.Test
{
    public class ContactPluginIntegrationTests
    {
        const string _primaryContactIdAttribute = "primarycontactid";
        private Guid _primaryContactAdvancedWirelessGuid => new System.Guid("0df81372-a2e8-ea11-a817-000d3a5a1477");
        private Guid _secondPrimaryContactAdvancedWirelessGuid => new System.Guid("55af2b77-9de9-ea11-a817-000d3a5a7103");
        private Guid _nonPrimaryContactAdvancedWirelessGuid => new System.Guid("55af2b77-9de9-ea11-a817-000d3a5a7103");
        private Guid _noAccountPrimaryContactGuid => new System.Guid("e9c09756-a0e9-ea11-a817-000d3a5a7103");
        private Guid _advancedWirelessAccount => new System.Guid("9d5dfc9c-68d2-ea11-a819-000d3a5913d3");
        
        private ITracingService _tracingService;
        private IOrganizationService _orgService;
        private DynamicsConfiguration _dynamicsConfiguration;
        private OrgServiceProvider _orgServiceProvider;
        private IContactService _contactService;
        private IAccountService _accountService;
        public class TracingService : ITracingService
        {
            public void Trace(string text, params object[] objectArr) {}
        }
        public ContactPluginIntegrationTests()
        {
            var configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json", true, true)
                .Build();

            _dynamicsConfiguration = configurationRoot.GetSection("Apogos").Get<DynamicsConfiguration>();
            _orgServiceProvider = new OrgServiceProvider();
            _tracingService = new TracingService();
        }

        [Fact]
        public async void SetAccountAsAccountPrimaryContactTest()
        {
            _orgService = await _orgServiceProvider.GetOrganizationService(_dynamicsConfiguration);
            _contactService = new ContactService(_orgService, _tracingService);
            _accountService = new AccountService(_orgService, _tracingService);

            var advancedWirelessAccount = (models.Account)_accountService.Get(_advancedWirelessAccount);
            RemovePrimaryContact(advancedWirelessAccount);

            var contactPlugin = new ContactCreation();
            var primaryContactAdvancedWireless = (models.Contact)_contactService.Get(_primaryContactAdvancedWirelessGuid);
            var nonPrimaryContactAdvancedWireless = (models.Contact)_contactService.Get(_nonPrimaryContactAdvancedWirelessGuid);
            var noAccountContact = (models.Contact)_contactService.Get(_noAccountPrimaryContactGuid);
            var secondPrimaryContactAdvancedWireless = (models.Contact)_contactService.Get(_secondPrimaryContactAdvancedWirelessGuid);

            // Attempting to set a Contact that does not have a contact type of Primary Contact as the Primary Contact on an Account should not throw,
            // and should not set the Account's Primary Contact.
            var accountAfterAttemptingToSetNonPrimaryContact = contactPlugin.SetContactAsAccountPrimaryContact(nonPrimaryContactAdvancedWireless);
            Assert.NotEqual(accountAfterAttemptingToSetNonPrimaryContact.GetAttributeValue<Guid>(_primaryContactIdAttribute), nonPrimaryContactAdvancedWireless.Id);

            var accountAfterSettingPrimaryContact = contactPlugin.SetContactAsAccountPrimaryContact(primaryContactAdvancedWireless);
            Assert.Equal(accountAfterSettingPrimaryContact.GetAttributeValue<Guid>(_primaryContactIdAttribute), primaryContactAdvancedWireless.Id);

            var noAccountContactShouldNotThrow = contactPlugin.SetContactAsAccountPrimaryContact(noAccountContact);

            // Attempting to set a Contact as a Primary Contact on an Account with an existing Primary Contact should not throw,
            // and should not overwrite the Account's Primary Contact
            var accountAfterAttemptingToSetAnotherPrimaryContact = contactPlugin.SetContactAsAccountPrimaryContact(secondPrimaryContactAdvancedWireless);
            Assert.Equal(accountAfterAttemptingToSetAnotherPrimaryContact.GetAttributeValue<Guid>(_primaryContactIdAttribute), primaryContactAdvancedWireless.Id);
        }

        private void RemovePrimaryContact(models.Account account)
        {
            account.SetAttribute(_primaryContactIdAttribute, null);
            _accountService.Update(account);
        }
    }
}
