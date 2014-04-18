using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Http;
using Zaz.Server.Advanced.Executor;
using Zaz.Server.Advanced.Service.Content;
using Zaz.Server.Advanced.Service.Contract;
using Zaz.Server.Advanced.State;

namespace Zaz.Server.Advanced.Service
{
    public class CommandsController : ApiController
    {
        readonly ServerContext _context;
        readonly CommandResolver _resolver;
        readonly CommandRunner _runner;
        readonly CommandsExecutor _executor;

        public CommandsController(ServerContext context)
        {
            _context = context ?? new ServerContext();
            _resolver = new CommandResolver(_context);
            _runner = new CommandRunner(_context);
            _executor = new CommandsExecutor(_context);
        }

        [HttpGet]
        public HttpResponseMessage Default(string path = "index.html")
        {
            return Accessor.Get(path);
        }

        [HttpPost]
        public HttpResponseMessage Default(PostCommandRequest env)
        {
            var cmdKey = env.Key;
            var cmd = _resolver.ResoveCommand(env, cmdKey);
            var msg = _runner.RunCommand(cmdKey, cmd, env.Tags);

            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Accepted,
                Content = new StringContent(msg)
            };
        }

        [HttpPost]
        public HttpResponseMessage PostLegacy(HttpRequestMessage req)
        {
            var body = req.Content.ReadAsStringAsync().Result;

            // Parsing QueryString
            var nvc = HttpUtility.ParseQueryString(body);
            var form = nvc.Keys.Cast<string>().ToDictionary(x => x, x => nvc[x]);

            if (!form.ContainsKey("Zaz-Command-Id"))
            {
                throw ExceptionsFactory.CreateApiException("Required value 'Zaz-Command-Id' was not found.");
            }

            var cmdKey = form["Zaz-Command-Id"];
            var cmdType = _resolver.ResolveCommandType(cmdKey);

            object cmd;

            try
            {
                var binder = new CommandBinder();
                cmd = binder.Build(cmdType, form);
            }
            catch (InvalidOperationException ex)
            {
                throw ExceptionsFactory.CreateApiException("Problems with binding command data. " + ex.Message);
            }

            var msg = _runner.RunCommand(cmdKey, cmd, new string[0]);
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Accepted,
                Content = new StringContent(msg)
            };
        }

        [HttpPost]
        public PostScheduledCommandResponse PostScheduled(PostCommandRequest req)
        {
            var cmdKey = req.Key;
            var cmd = _resolver.ResoveCommand(req, cmdKey);

            var id = _executor.Submit(cmd, req.Tags, Thread.CurrentPrincipal);

            return new PostScheduledCommandResponse
            {
                Id = id.ToString()
            };
        }

        [HttpGet]
        public IQueryable<CommandMeta> MetaList()
        {
            var registry = (_context.Registry ?? Implementations.CommandRegistry.Value);
            return registry
                .Query()
                .Select(x => new CommandMeta
                {
                    Key = x.Key,
                    Description = x.Description,
                    Aliases = x.Aliases,
                    Tags = x.Tags,
                    Parameters = x.Parameters
                                  .Select(xx => new CommandMetaParameter
                                  {
                                      Name = xx.Name,
                                      Description = xx.Description,
                                      Type = xx.Type
                                  })
                                  .ToArray()
                });
        }

        [HttpGet]
        public ExecutionStats GetScheduled(string id, DateTime? token)
        {
            var executionId = ExecutionId.FromString(id);
            return _executor.GetExecutionStats(executionId, token);
        }

        [HttpGet]
        public IQueryable<LogEntry> GetScheduledLog(string id)
        {
            var stateProvider = (_context.StateProvider ?? Implementations.StateProvider.Value);
            return stateProvider
                .QueryEntries(id)
                .Where(x => x.Kind == ProgressEntryKind.Trace)
                .Select(ConvertLogEntry());
        }

        public static Expression<Func<ProgressEntry, LogEntry>> ConvertLogEntry()
        {
            return e => new LogEntry
            {
                Message = e.Message,
                Severity = e.Severity,
                Tags = e.Tags,
                Timestamp = e.Timestamp
            };
        }
    }
}
