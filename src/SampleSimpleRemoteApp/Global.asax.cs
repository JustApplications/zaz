﻿using System;
using System.Linq;
using System.Web;
using SampleCommands;
using SampleHandlers;
using Zaz.Server;

namespace SampleEasyRemoteApp
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            ZazServer.Init("Commands",                
                new Conventions
                {
                    CommandRegistry = new ReflectionCommandRegistry(typeof(__SampleCommandsMarker).Assembly),
                    CommandBroker = new ReflectionCommandBroker(typeof(__SampleHandlersMarker).Assembly)
                });
        }        
    }
}