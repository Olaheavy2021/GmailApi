using GmailAPI.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GmailAPI.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class GmailController : ControllerBase
    {
        private readonly IGmailSpool _gmailSpool;
        public GmailController(IGmailSpool gmailSpool)
        {
            _gmailSpool = gmailSpool;
        }
        [HttpGet("FetchMailsFromGmail")]
        public async Task<IActionResult> FetchFromGmail([FromQuery]string hostName)
        {
            var response = await  _gmailSpool.GetMailsFromGmailAsync(hostName);

            return Ok(response);
        }

        [HttpGet("FetchMailsFromDb")]
        public IActionResult FetchFromDb([FromQuery] string bankName)
        {
            var response = _gmailSpool.GetMailsFromDb(bankName);

            return Ok(response);
        }

        [HttpGet("FetchTransactionDetails")]
        public IActionResult FetchTransactionDetails([FromQuery] string bankName)
        {
            var response = _gmailSpool.GetTransactionDetailsFromDb(bankName);

            return Ok(response);
        }
    }
}
