using GmailAPI.ApiHelper;
using GmailAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GmailAPI.Interface
{
    public interface IGmailSpool
    {
        Task<List<Gmail>> GetMailsFromGmailAsync(string hostname);

        List<Mail> GetMailsFromDb(string bankName);
    }
}
