﻿using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Web.BackOffice.Controllers;

using uSync.BackOffice.Hubs;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Controllers
{
    /// <summary>
    ///  Actions by handler, allows for things to be split.
    /// </summary>
    public partial class uSyncDashboardApiController : UmbracoAuthorizedJsonController
    {

        /// <summary>
        ///  Get the list of handler aliases to use for any give action
        /// </summary>
        [HttpPost]
        public IEnumerable<SyncHandlerView> GetActionHandlers([FromQuery]HandlerActions action, uSyncOptions options)
        {
            var handlerGroup = string.IsNullOrWhiteSpace(options.Group)
                ? _uSyncConfig.Settings.UIEnabledGroups
                : options.Group;

            var handlerSet = string.IsNullOrWhiteSpace(options.Set) 
                ? _uSyncConfig.Settings.DefaultSet
                : options.Set;

            return _handlerFactory.GetValidHandlers(new SyncHandlerOptions
            {
                Group = handlerGroup,
                Action = action,
                Set = handlerSet
            }).Select(x => new SyncHandlerView
            {
                Enabled = x.Handler.Enabled,
                Alias = x.Handler.Alias,
                Name = x.Handler.Name,
                Icon = x.Handler.Icon,
                Group = x.Handler.Group,
                Set = handlerSet
            });
        }

        /// <summary>
        ///  run the report process against a given handler alias
        /// </summary>
        [HttpPost]
        public SyncActionResult ReportHandler(SyncActionOptions options)
        {
            var hubClient = new HubClientService(_hubContext, options.ClientId);

            var handlerSet = !string.IsNullOrWhiteSpace(options.Set)
                ? options.Set : _uSyncConfig.Settings.DefaultSet;

            var actions = _uSyncService.ReportHandler(options.Handler,
                new uSyncImportOptions
                {
                    Callbacks = hubClient.Callbacks(),
                    HandlerSet = handlerSet,
                    RootFolder = _uSyncConfig.GetRootFolder(),                    
                });

            return new SyncActionResult(actions);
        }

        /// <summary>
        ///  Run the import against a given handler alias
        /// </summary>
        /// <remarks>
        ///  Will use the list of passed handler aliases to only import those alias values.
        ///  this allows us to break up the import into more requests.
        /// </remarks>
        [HttpPost]
        public SyncActionResult ImportHandler(SyncActionOptions options)
        {
            var hubClient = new HubClientService(_hubContext, options.ClientId);

            var handlerSet = !string.IsNullOrWhiteSpace(options.Set)
                ? options.Set : _uSyncConfig.Settings.DefaultSet;

            var actions = _uSyncService.ImportHandler(options.Handler, new uSyncImportOptions
            { 
                Callbacks = hubClient.Callbacks(),
                HandlerSet = handlerSet,
                RootFolder = _uSyncConfig.GetRootFolder(),
                PauseDuringImport = true,
                Flags = options.Force ? Core.Serialization.SerializerFlags.Force : Core.Serialization.SerializerFlags.None
            });

            return new SyncActionResult(actions);
        }

        /// <summary>
        ///  run the post import options (called after all the handlers you are going to use)
        /// </summary>
        [HttpPost]
        public SyncActionResult ImportPost(SyncActionOptions options)
        {
            var hubClient = new HubClientService(_hubContext, options.ClientId);

            var handlerSet = !string.IsNullOrWhiteSpace(options.Set)
                ? options.Set : _uSyncConfig.Settings.DefaultSet;

            var actions = _uSyncService.PerformPostImport(
                _uSyncConfig.GetRootFolder(), 
                handlerSet, 
                options.Actions);

            return new SyncActionResult(actions);
        }


        /// <summary>
        ///  clean the export folder, 
        /// </summary>
        [HttpPost]
        public bool CleanExport()
        {
            try
            {
                return _uSyncService.CleanExportFolder(_uSyncConfig.GetRootFolder());
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///  export all the items for a given handler
        /// </summary>
        [HttpPost]
        public SyncActionResult ExportHandler(SyncActionOptions options)
        {
            var hubClient = new HubClientService(_hubContext, options.ClientId);

            var handlerSet = !string.IsNullOrWhiteSpace(options.Set)
                ? options.Set : _uSyncConfig.Settings.DefaultSet;


            var actions = _uSyncService.ExportHandler(options.Handler, new uSyncImportOptions
            {
                Callbacks = hubClient.Callbacks(),
                HandlerSet = handlerSet,
                RootFolder = _uSyncConfig.GetRootFolder()
            });

            return new SyncActionResult(actions);
        }

        /// <summary>
        ///  trigger the start of a bulk process (fires events)
        /// </summary>
        /// <param name="action"></param>
        [HttpPost]
        public void StartProcess([FromQuery]HandlerActions action)
            => _uSyncService.StartBulkProcess(action);

        /// <summary>
        ///  trigger the end of a bulk process (fire events)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actions"></param>
        [HttpPost]
        public void FinishProcess([FromQuery]HandlerActions action, IEnumerable<uSyncAction> actions)
            => _uSyncService.FinishBulkProcess(action, actions);
    }
}
