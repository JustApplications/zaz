﻿using System;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using NUnit.Framework;
using Zaz.Client;
using Zaz.Server.Advanced;
using Zaz.Server.Advanced.Broker;
using Zaz.Server.Advanced.Service;
using Zaz.Tests.Stubs;
using FluentAssertions;

namespace Zaz.Tests.Integration.Errors
{
    public class When_posting_command_to_server_that_fails
    {
        
        static readonly string URL = "http://" + FortyTwo.LocalHost + ":9303/FailingServerCommands/";        
                
        Exception _resultEx;

        [TestFixtureSetUp]
        public void Given_command_server_runnig()
        {
            var instance = new CommandsService(new ServerContext
            (
                registry: new FooCommandRegistry(),
                broker: new DelegatingCommandBroker((cmd, ctx) =>
                {
                    throw new InvalidOperationException("Server failed...");
                    return Task.Factory.StartNew(() => {});
                })
            ));
            using (var host = instance.OpenConfiguredServiceHost(URL))
            {            
                var serviceDebugBehaviour = host.Description.Behaviors.Find<ServiceDebugBehavior>();
                serviceDebugBehaviour.IncludeExceptionDetailInFaults = true;

                // Client side
                var bus = new ZazClient(URL);
                try
                {
                    bus.Post(new FooCommand
                    {
                        Message = "Hello world"
                    });
                }
                catch (Exception ex)
                {
                    _resultEx = ex;
                }
            }  
        }
        
        [Test]
        public void Should_throw_exception()
        {
            _resultEx.Should().NotBeNull();
        }

        [Test]
        public void Should_throw_with_details()
        {
            _resultEx.ToString().Should().Contain("Server failed...");
        }

        [Test]
        public void Should_be_zaz_exception()
        {
            _resultEx.Should().BeAssignableTo<ZazException>();
        }        
    }
}