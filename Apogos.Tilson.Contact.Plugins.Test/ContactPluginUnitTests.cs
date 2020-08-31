using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Apogos.Tilson.Contact.Plugins;
using Models = Apogos.Dynamics.Common.Models;

namespace Apogos.Tilson.Contact.Plugins.Test
{
    public class ContactPluginUnitTests
    {
        private const string _primaryContactIdValueAttribute = "_primarycontactid_value";
        private const string _contactTypeAttribute = "apogos_contacttype";
        private const int _primaryContactType = 255290001;
        private readonly ContactCreation _contactCreation;

        public ContactPluginUnitTests()
        {
            _contactCreation = new ContactCreation();
        }

        [Fact]
        public void ShouldSetAccountPrimaryContactIfContactIsPrimary()
        {
            var primaryContact = new Models.Contact();
            primaryContact.Id = System.Guid.NewGuid();
            primaryContact.SetAttribute(_contactTypeAttribute, _primaryContactType);

            var account = new Models.Account();
            account.SetAttribute(_primaryContactIdValueAttribute, null);
            var updatedAccount = _contactCreation.SetContactAsAccountPrimaryContact(primaryContact);

            Assert.Equal(primaryContact.Id, updatedAccount.GetAttributeValue<Guid>(_primaryContactIdValueAttribute));
        }
    }
}
