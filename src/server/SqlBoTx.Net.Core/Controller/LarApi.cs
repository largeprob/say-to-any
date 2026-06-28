using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SqlBoTx.Net.Core.Controller
{
    /// <summary>
    /// ApiController
    /// </summary>
    [ApiController]
    [Authorize]
    public abstract class LarApi : ControllerBase
    {

    }
}
