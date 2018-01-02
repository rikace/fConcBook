using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockTicker.Server.Cs
{
    public class ErrorHandlingPipelineModule : HubPipelineModule
    {
        public ErrorHandlingPipelineModule() { }

        protected override void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
        {
            System.Diagnostics.Debug.WriteLine("=> Exception " + exceptionContext.Error.Message);
            if (exceptionContext.Error.InnerException != null)
                System.Diagnostics.Debug.WriteLine("=> Inner Exception " + exceptionContext.Error.InnerException.Message);
            base.OnIncomingError(exceptionContext, invokerContext);
        }
    }
}