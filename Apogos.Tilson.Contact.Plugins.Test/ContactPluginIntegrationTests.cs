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

namespace Apogos.Tilson.Contact.Plugins.Test
{
    public class ContactPluginIntegrationTests
    {
        private const string _primaryContactIdAttribute = "primarycontactid";
        private const string _primaryContactIdValueAttribute = "_primarycontactid_value";
        private const string _parentCustomerIdAttribute = "parentcustomerid";
        private const string _contactTypeAttribute = "apogos_contacttype";
        private const int _primaryContactType = 255290001;
        private Guid _primaryContactAdvancedWirelessGuid => new System.Guid("0df81372-a2e8-ea11-a817-000d3a5a1477");
        private Guid _secondPrimaryContactAdvancedWirelessGuid => new System.Guid("55af2b77-9de9-ea11-a817-000d3a5a7103");
        private Guid _nonPrimaryContactAdvancedWirelessGuid => new System.Guid("55af2b77-9de9-ea11-a817-000d3a5a7103");
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
            _orgServiceProvider = new OrgServiceProvider();
            _tracingService = new TracingService();
            _orgService = _orgServiceProvider.GetOrganizationService(_dynamicsConfiguration).Result;
            _contactService = new ContactService(_orgService, _tracingService);
            _accountService = new AccountService(_orgService, _tracingService);
            _advancedWirelessAccount = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);
        }

        private void RemovePrimaryContact(Models.Account account)
        {
            account.SetAttribute(_primaryContactIdAttribute, null);
            _accountService.Update(account);
        }

        [Fact]
        public void ShouldSetAccountPrimaryContactIfContactIsPrimary()
        {
            var primaryContact = (Models.Contact)_contactService.Get(_primaryContactAdvancedWirelessGuid);
            Assert.Contains(_primaryContactType.ToString(), primaryContact.GetAttributeValue<string>(_contactTypeAttribute));

            _contactPlugin.TargetEntity = primaryContact.TargetEntity;
            RemovePrimaryContact(_advancedWirelessAccount);
            Assert.Null(_advancedWirelessAccount.GetAttribute(_primaryContactIdAttribute));

            _contactPlugin.Process();
            var account = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);
            Assert.Equal(primaryContact.Id, account.GetAttribute(_primaryContactIdValueAttribute));
        }

        [Fact]
        public void ShouldNotSetAccountPrimaryContactIfContactIsNotPrimary()
        {
            var nonPrimaryContact = (Models.Contact)_contactService.Get(_nonPrimaryContactAdvancedWirelessGuid);
            _contactPlugin.TargetEntity = nonPrimaryContact.TargetEntity;

            RemovePrimaryContact(_advancedWirelessAccount);
            Assert.Null(_advancedWirelessAccount.GetAttribute(_primaryContactIdAttribute));

            _contactPlugin.Process();
            var updatedAccount = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);
            Assert.Null(updatedAccount.GetAttribute(_primaryContactIdValueAttribute));
        }

        [Fact]
        public void ShouldNotOverwriteExistingAccountPrimaryContact()
        {
            var account = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);
            var existingPrimaryContactId = account.GetAttributeValue<Guid>(_primaryContactIdValueAttribute);

            if (existingPrimaryContactId == null)
            {
                ShouldSetAccountPrimaryContactIfContactIsPrimary();
                account = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);
                existingPrimaryContactId = account.GetAttributeValue<Guid>(_primaryContactIdValueAttribute);
            }

            Assert.NotNull(account.GetAttributeValue<Guid?>(_primaryContactIdValueAttribute));

            var primaryContact = (Models.Contact)_contactService.Get(_secondPrimaryContactAdvancedWirelessGuid);
            _contactPlugin.TargetEntity = primaryContact.TargetEntity;
            _contactPlugin.Process();
            var updatedAccount = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);
            Assert.Equal(existingPrimaryContactId, updatedAccount.GetAttributeValue<Guid>(_primaryContactIdValueAttribute));

            Assert.NotNull(account.GetAttributeValue<Guid?>(_primaryContactIdValueAttribute));

            var nonPrimaryContact = (Models.Contact)_contactService.Get(_nonPrimaryContactAdvancedWirelessGuid);
            _contactPlugin.TargetEntity = nonPrimaryContact.TargetEntity;
            _contactPlugin.Process();
            updatedAccount = (Models.Account)_accountService.Get(_advancedWirelessAccountGuid);
            Assert.Equal(existingPrimaryContactId, updatedAccount.GetAttributeValue<Guid>(_primaryContactIdValueAttribute));
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
