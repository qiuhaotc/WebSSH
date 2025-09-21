using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebSSH.Shared;

namespace WebSSH.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ShellController : ControllerBase
    {
        public ShellPool ShellPool { get; }

        public ShellController(ShellPool shellPool)
        {
            ShellPool = shellPool;
        }

        [HttpPost]
        public ServerResponse<ActiveSessionModel> Connected(ActiveSessionModel activeSessionModel)
        {
            var id = HttpContext.Session.GetString(Constants.ClientSessionIdName);
            var response = new ServerResponse<ActiveSessionModel> { StausResult = StausResult.Successful };

            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                HttpContext.Session.SetString(Constants.ClientSessionIdName, id);
            }

            try
            {
                if (activeSessionModel.StoredSessionModel != null)
                {
                    ShellPool.AddShellToPool(id, activeSessionModel);

                    response.Response = new ActiveSessionModel
                    {
                        StartSessionDate = DateTime.Now,
                        Status = "Connected Successful",
                        StoredSessionModel = activeSessionModel.StoredSessionModel,
                        UniqueKey = activeSessionModel.UniqueKey
                    };
                }
            }
            catch (Exception ex)
            {
                response.StausResult = StausResult.Exception;
                response.ExtraMessage = ex.Message;
            }

            return response;
        }

        // Removed RunShellCommand, GetShellOutput, IsConnected after migrating to SignalR push model


        public ServerResponse<bool> IsConnected(Guid uniqueId)
        {
            var id = HttpContext.Session.GetString(Constants.ClientSessionIdName);
            var response = new ServerResponse<bool> { StausResult = StausResult.Successful };

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "No active sessions";
                }
                else
                {
                    response.Response = ShellPool.IsConnected(id, uniqueId);
                }
            }
            catch (Exception ex)
            {
                response.StausResult = StausResult.Exception;
                response.ExtraMessage = ex.Message;
            }

            return response;
        }

        public ServerResponse<bool> Disconnected(Guid uniqueId)
        {
            var id = HttpContext.Session.GetString(Constants.ClientSessionIdName);
            var response = new ServerResponse<bool> { StausResult = StausResult.Successful };

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "No active sessions";
                }
                else
                {
                    response.Response = ShellPool.Disconnected(id, uniqueId);
                }
            }
            catch (Exception ex)
            {
                response.StausResult = StausResult.Exception;
                response.ExtraMessage = ex.Message;
            }

            return response;
        }

        public ServerResponse<List<ActiveSessionModel>> ConnectedSessions()
        {
            var id = HttpContext.Session.GetString(Constants.ClientSessionIdName);
            var response = new ServerResponse<List<ActiveSessionModel>> { StausResult = StausResult.Successful };

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    response.Response = new List<ActiveSessionModel>();
                }
                else
                {
                    response.Response = ShellPool.GetConnectedSessionsAndClearNotConnected(id);
                }
            }
            catch (Exception ex)
            {
                response.StausResult = StausResult.Exception;
                response.ExtraMessage = ex.Message;
            }

            return response;
        }
    }
}
