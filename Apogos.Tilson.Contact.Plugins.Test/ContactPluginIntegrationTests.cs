using Apogos.Dynamics.Common.Configuration;
using Models = Apogos.Dynamics.Common.Models;
using Apogos.Dynamics.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using Xunit;
using Apogos.Dynamics.Common.Interfaces;
using System.Windows.Forms;
using System.Net;

namespace Apogos.Tilson.Contact.Plugins.Test
{
    public class ContactPluginIntegrationTests
    {
        private const string _primaryContactIdAttribute = "primarycontactid";
        private const string _parentCustomerIdAttribute = "parentcustomerid";
        private const string _contactTypeAttribute = "apogos_contacttype";
        private const int _primaryContactType = 255290001;
        private Guid _primaryContactAdvancedWirelessGuid => new System.Guid("0df81372-a2e8-ea11-a817-000d3a5a1477");
        private Guid _secondPrimaryContactAdvancedWirelessGuid => new System.Guid("55af2b77-9de9-ea11-a817-000d3a5a7103");
        private Guid _nonPrimaryContactAdvancedWirelessGuid => new System.Guid("9a041a91-9fe9-ea11-a817-000d3a5a7103");
        private Guid _noAccountPrimaryContactGuid => new System.Guid("e9c09756-a0e9-ea11-a817-000d3a5a7103");
        private Guid _advancedWirelessAccountGuid => new System.Guid("9d5dfc9c-68d2-ea11-a819-000d3a5913d3");
        
        private ITracingService _tracingService;
        private IOrganizationService _orgService;
        private DynamicsConfiguration _dynamicsConfiguration;
        private OrgServiceProvider _orgServiceProvider;
        private IContactService _contactService;
        private IAccountService _accountService;
        private ContactCreation _contactPlugin;
        private Models.Account _advancedWirelessAccount;
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

            // Manually setting the security protocol to TLS 1.2 because the default setting 1.0 causes a connection error. https://alphabold.com/tls-1-2-and-dynamics-365-connectivity-issues/
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _orgServiceProvider = new OrgServiceProvider();
            _tracingService = new TracingService();
            _orgService = _orgServiceProvider.GetOrganizationService(_dynamicsConfiguration).Result;
            _contactService = new ContactService(_orgService, _tracingService);
            _accountService = new AccountService(_orgService, _tracingService);
            _advancedWirelessAccount = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);

            _contactPlugin = new ContactCreation();
        }

        private void RemovePrimaryContact(Models.Account account)
        {
            account.SetAttribute(_primaryContactIdAttribute, null);
            _accountService.Update(account);
        }

        private void SetPrimaryContact(Models.Account account)
        {
            var contactReference = new EntityReference(new Models.Contact().EntityName,  _primaryContactAdvancedWirelessGuid);
            account.SetAttribute(_primaryContactIdAttribute, contactReference);
            _accountService.Update(account);
        }

        [Fact]
        public void ShouldSetAccountPrimaryContactIfContactIsPrimary()
        {
            var primaryContact = (Models.Contact)_contactService.Get(_primaryContactAdvancedWirelessGuid);
            var primaryContactTypeOptionSet = new OptionSetValue(_primaryContactType);
            Assert.Contains(primaryContactTypeOptionSet, primaryContact.GetAttributeValue<OptionSetValueCollection>(_contactTypeAttribute));

            _contactPlugin.TargetEntity = primaryContact.TargetEntity;
            RemovePrimaryContact(_advancedWirelessAccount);
            Assert.Null(_advancedWirelessAccount.GetAttribute(_primaryContactIdAttribute));

            _contactPlugin.SetContactAsAccountPrimaryContact(_accountService, _advancedWirelessAccountGuid, primaryContact);
            var account = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);
            Assert.Equal(primaryContact.Id, account.GetAttributeValue<EntityReference>(_primaryContactIdAttribute).Id);
        }

        [Fact]
        public void ShouldNotThrowIfContactIsNotPrimary()
        {
            var nonPrimaryContact = (Models.Contact)_contactService.Get(_nonPrimaryContactAdvancedWirelessGuid);
            _contactPlugin.TargetEntity = nonPrimaryContact.TargetEntity;

            RemovePrimaryContact(_advancedWirelessAccount);
            Assert.Null(_advancedWirelessAccount.GetAttribute(_primaryContactIdAttribute));

            _contactPlugin.Process();
        }

        [Fact]
        public void ShouldNotOverwriteExistingAccountPrimaryContact()
        {
            var account = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);
            var existingPrimaryContact = account.GetAttributeValue<EntityReference>(_primaryContactIdAttribute);

            if (existingPrimaryContact == null)
            {
                SetPrimaryContact(account);
                existingPrimaryContact = account.GetAttributeValue<EntityReference>(_primaryContactIdAttribute);
            }

            Assert.NotNull(account.GetAttributeValue<EntityReference>(_primaryContactIdAttribute));

            var primaryContact = (Models.Contact)_contactService.Get(_secondPrimaryContactAdvancedWirelessGuid);
            _contactPlugin.TargetEntity = primaryContact.TargetEntity;
            _contactPlugin.SetContactAsAccountPrimaryContact(_accountService, _advancedWirelessAccountGuid, primaryContact);

            var updatedAccount = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);
            Assert.Equal(existingPrimaryContact.Id, updatedAccount.GetAttributeValue<EntityReference>(_primaryContactIdAttribute).Id);
        }

        [Fact]
        public void ShouldNotThrowIfContactAccountValueIsNull()
        {
            var noAccountContact = (Models.Contact)_contactService.Get(_noAccountPrimaryContactGuid);
            _contactPlugin.TargetEntity = noAccountContact.TargetEntity;

            Assert.Null(noAccountContact.GetAttributeValue<Guid?>(_parentCustomerIdAttribute));

            _contactPlugin.Process();
        }
    }
}
