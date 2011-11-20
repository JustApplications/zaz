﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.ApplicationServer.Http;
using Microsoft.ApplicationServer.Http.Activation;
using NUnit.Framework;
using SampleCommands;
using SampleHandlers;
using Zaz.Client;
using Zaz.Server;
using Zaz.Server.Advanced;
using Zaz.Server.Advanced.Broker;
using Zaz.Server.Advanced.Registry;
using Zaz.Server.Advanced.Service;
using Zaz.Server.Advanced.Service.Security;
using Zaz.Tests.Stubs;
using FluentAssertions;

namespace Zaz.Tests.Integration.Security
{
    public class When_posting_command_to_server_with_basic_auth
    {
        private HttpServiceHost _host;

        private const string URL = "http://localhost.fiddler:9303/BasicAuthCommands/";

        private object _postedCommand;
        private CommandHandlingContext _ctx;

        [TestFixtureSetUp]
        public void Given_command_server_runnig()
        {
            var instance = new CommandsService(new ServerContext
            (
                registry: new FooCommandRegistry(),
                broker: new DelegatingCommandBroker((cmd, ctx) =>
                {
                    _postedCommand = cmd;
                    _ctx = ctx;                    

                    return Task.Factory.StartNew(() => {});
                })
            ));            
            var config = ConfigurationHelper.CreateConfiguration(instance);

            SimpleBasicAuthenticationHandler.Configure(config, cred => cred.UserName == "supr" && cred.Password == "booper");

            _host = new HttpServiceHost(typeof(CommandsService), config, new Uri(URL));
            _host.Open();


            // Client side

            var bus = new CommandBus(URL, new Client.Avanced.ZazConfiguration
            {
                ConfigureHttp = h =>
                {
                    h.Credentials = new NetworkCredential("supr", "booper");
                }
            });
            bus.Post(new FooCommand
            {
                Message = "Hello world"
            });

            // Close host
            _host.Close();

        }
        
        [Test]
        public void Should_successfully_send_command()
        {
            _postedCommand.Should().NotBeNull();
        }

        [Test]
        public void Should_be_authenticated()
        {
            _ctx.Principal.Identity.IsAuthenticated.Should().BeTrue();
        }

        [Test]
        public void Should_be_authenticated_with_correct_user()
        {
            _ctx.Principal.Identity.Name.Should().Be("supr");
        }
    }
}