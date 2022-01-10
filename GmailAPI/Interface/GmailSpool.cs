using AutoMapper;
using GmailAPI.ApiHelper;
using GmailAPI.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GmailAPI.Interface
{
    public class GmailSpool : IGmailSpool
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;
        public GmailSpool(DataContext dataContext)
        {
            _dataContext = dataContext;
        }
        public async Task<List<Gmail>> GetMailsFromGmailAsync(string hostname)
        {
            try
            {
                var hostEmailAddress = ConfigurationManager.AppSettings["HostAddress"];
                var bankEmail = ConfigurationManager.AppSettings["BankEmail"];
                var bankSubject = ConfigurationManager.AppSettings["BankSubject"];
                var bankName = ConfigurationManager.AppSettings["BankName"];



                GmailService GmailService = GmailAPIHelper.GetService();
                List<Gmail> EmailList = new List<Gmail>();
                UsersResource.MessagesResource.ListRequest ListRequest = GmailService.Users.Messages.List(hostEmailAddress);

                //query parameters
                DateTime after = DateTime.Now.AddDays(-1);
                DateTime before = DateTime.Now.AddDays(2);

                var afterString = after.ToString("yyyy/MM/dd");
                var beforeString = before.ToString("yyyy/MM/dd");

                ListRequest.LabelIds = "INBOX";
                ListRequest.IncludeSpamTrash = false;
                //ListRequest.Q = $"is:unread from:{bankEmail} subject:{bankSubject}"; //ONLY FOR UNDREAD EMAIL'S...

                //ListRequest.Q = $"is:unread from:{bankEmail} subject:{bankSubject} after:{afterString} before:{beforeString}";

                ListRequest.Q = $"from:{bankEmail} subject:{bankSubject} after:{afterString} before:{beforeString}";

                //ListRequest.Q = $"is:unread from:{bankEmail} subject:{bankSubject}";

                //GET ALL EMAILS
                ListMessagesResponse ListResponse = ListRequest.Execute();

                if (ListResponse != null && ListResponse.Messages != null)
                {
                    //LOOP THROUGH EACH EMAIL AND GET WHAT FIELDS I WANT
                    foreach (Message Msg in ListResponse.Messages)
                    {
                        //MESSAGE MARKS AS READ AFTER READING MESSAGE
                        GmailAPIHelper.MsgMarkAsRead(hostEmailAddress, Msg.Id);

                        UsersResource.MessagesResource.GetRequest Message = GmailService.Users.Messages.Get(hostEmailAddress, Msg.Id);
                        Console.WriteLine("\n-----------------NEW MAIL----------------------");
                        Console.WriteLine("STEP-1: Message ID:" + Msg.Id);

                        //MAKE ANOTHER REQUEST FOR THAT EMAIL ID...
                        Message MsgContent = Message.Execute();

                        if (MsgContent != null)
                        {
                            string FromAddress = string.Empty;
                            string Date = string.Empty;
                            string Subject = string.Empty;
                            string MailBody = string.Empty;
                            string ReadableText = string.Empty;

                            //LOOP THROUGH THE HEADERS AND GET THE FIELDS WE NEED (SUBJECT, MAIL)
                            foreach (var MessageParts in MsgContent.Payload.Headers)
                            {
                                if (MessageParts.Name == "From")
                                {
                                    FromAddress = MessageParts.Value;
                                }
                                else if (MessageParts.Name == "Date")
                                {
                                    Date = MessageParts.Value;
                                }
                                else if (MessageParts.Name == "Subject")
                                {
                                    Subject = MessageParts.Value;
                                }
                            }
                            //READ MAIL BODY
                            Console.WriteLine("STEP-2: Read Mail Body");
                            List<string> FileName = GmailAPIHelper.GetAttachments(hostEmailAddress, Msg.Id, Convert.ToString(ConfigurationManager.AppSettings["GmailAttach"]));

                            if (FileName.Count() > 0)
                            {
                                foreach (var EachFile in FileName)
                                {
                                    //GET USER ID USING FROM EMAIL ADDRESS-------------------------------------------------------
                                    string[] RectifyFromAddress = FromAddress.Split(' ');
                                    string FromAdd = RectifyFromAddress[RectifyFromAddress.Length - 1];

                                    if (!string.IsNullOrEmpty(FromAdd))
                                    {
                                        FromAdd = FromAdd.Replace("<", string.Empty);
                                        FromAdd = FromAdd.Replace(">", string.Empty);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("STEP-3: Mail has no attachments.");
                            }

                            //READ MAIL BODY-------------------------------------------------------------------------------------
                            MailBody = string.Empty;
                            if (MsgContent.Payload.Parts == null && MsgContent.Payload.Body != null)
                            {
                                MailBody = MsgContent.Payload.Body.Data;
                            }
                            else
                            {
                                MailBody = GmailAPIHelper.MsgNestedParts(MsgContent.Payload.Parts);
                            }

                            //BASE64 TO READABLE TEXT--------------------------------------------------------------------------------
                            ReadableText = string.Empty;
                            ReadableText = GmailAPIHelper.Base64Decode(MailBody);

                            Console.WriteLine("STEP-4: Identifying & Configure Mails.");

                            if (!string.IsNullOrEmpty(ReadableText))
                            {
                                Gmail GMail = new Gmail();
                                GMail.From = FromAddress;
                                GMail.Body = ReadableText;
                                GMail.MailDateTime = Convert.ToDateTime(Date);
                                GMail.MsgID = Msg.Id;
                                GMail.To = hostEmailAddress;
                                

                                //check if email already exists.
                                if(!await CheckIfMailExists(GMail.MsgID))
                                {
                                    //save into the database 
                                    await CreateMailAsync(GMail, bankName);
                                }
                                

                                //Add to the list
                                EmailList.Add(GMail);
                            }

                            

                        }
                    }
                }
                return EmailList;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);

                return null;
            }

        }

        private async Task<bool> CreateMailAsync(Gmail gmail, string BankName)
        {
            //var mailToBeSaved = _mapper.Map<Mail>(gmail);

           var mailToBeSaved = new Mail();


            mailToBeSaved.Status = "Initiated";
            mailToBeSaved.MailDateTime = gmail.MailDateTime;
            mailToBeSaved.To = gmail.To;
            mailToBeSaved.From = gmail.From;
            mailToBeSaved.Body = gmail.Body;
            mailToBeSaved.Bank = BankName;
            mailToBeSaved.Status = "Initiated";

            await _dataContext.AddAsync(mailToBeSaved);

            var result = await  _dataContext.SaveChangesAsync();

            return result > 0;
        }

        private async Task<bool> CheckIfMailExists(string messageId)
        {
            var mailFromDb = await  _dataContext.Mail.FirstOrDefaultAsync(m => m.MsgID == messageId);

            if(mailFromDb != null)
            {
                return true;
            }

            return false;


        }

        public List<Mail> GetMailsFromDb(string bankName)
        {
            var mailsFromDb = _dataContext.Mail.Where(x => x.Bank == bankName).ToList();

            if(mailsFromDb != null)
            {
                return mailsFromDb;
            }

            return new List<Mail> { };
        }

        
    }
}
