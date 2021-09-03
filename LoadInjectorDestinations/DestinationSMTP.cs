using LoadInjectorBase;
using MimeKit;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using MailKit.Net.Smtp;

using MimeKit;
using NLog;
using System;
using System.Collections.Generic;

using System.ComponentModel;

using System.IO;

using System.Text;
using System.Xml;

using System.Text;
using System.Xml;

namespace LoadInjector.Destinations
{
    public class DestinationSMTP : DestinationAbstract
    {
        private string smtphost;
        private int smtpport;
        private string smtpuser;
        private string smtppass;
        private bool smtpuseSSL = false;
        private bool smtpAttachment = false;
        private string smtpsubject;
        private string smtpfromUser;
        private string smtpfromEmail;
        private string smtptoUser;
        private string smtptoEmail;
        private string smtpattachname;

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log)
        {
            base.Configure(node, cont, log);

            try
            {
                smtphost = node.Attributes["host"]?.Value;
            }
            catch (Exception)
            {
                Console.WriteLine($"SMTP not defined for {node.Attributes["name"].Value}");
                return false;
            }

            try
            {
                smtpport = int.Parse(node.Attributes["port"].Value);
            }
            catch (Exception)
            {
                Console.WriteLine($"No SMTP Port correctly defined for {node.Attributes["name"].Value}");
                return false; ;
            }

            try
            {
                smtpuser = node.Attributes["username"].Value;
            }
            catch (Exception)
            {
                smtpuser = null;
            }

            try
            {
                smtpattachname = node.Attributes["smtpattachname"].Value;
            }
            catch (Exception)
            {
                smtpattachname = "LoadInjectorMsg";
            }

            try
            {
                smtppass = node.Attributes["password"].Value;
            }
            catch (Exception)
            {
                smtppass = null;
            }

            try
            {
                smtpsubject = node.Attributes["smtpsubject"].Value;
            }
            catch (Exception)
            {
                smtpsubject = "Load Injector Message";
            }

            try
            {
                smtpfromUser = node.Attributes["smtpfromUser"].Value;
            }
            catch (Exception)
            {
                smtpfromUser = "Load Injector";
            }

            try
            {
                smtpfromEmail = node.Attributes["smtpfromEmail"].Value;
            }
            catch (Exception)
            {
                smtpfromEmail = "";
            }

            try
            {
                smtptoUser = node.Attributes["smtpToUser"].Value;
            }
            catch (Exception)
            {
                smtptoUser = "Load Injector Recipient";
            }

            try
            {
                smtptoEmail = node.Attributes["smtpToEmail"].Value;
            }
            catch (Exception)
            {
                Console.WriteLine($"No SMTP Destination Email defined for {node.Attributes["name"].Value}");
                return false; ;
            }

            try
            {
                smtpuseSSL = bool.Parse(node.Attributes["smtpuseSSL"].Value);
            }
            catch (Exception)
            {
                smtpuseSSL = false; ;
            }

            try
            {
                smtpuseSSL = bool.Parse(node.Attributes["smtpAttachment"].Value);
            }
            catch (Exception)
            {
                smtpAttachment = false; ;
            }

            return true;

            return true;
        }

        public override string GetDestinationDescription()
        {
            return $"Host: {smtphost}, From Email : {smtpfromEmail}";
        }

        public override bool Send(string message, List<Variable> vars)
        {
            string subject = smtpsubject;
            foreach (Variable v in vars)
            {
                try
                {
                    subject = subject.Replace(v.token, v.value);
                    smtpattachname = subject.Replace(v.token, v.value);
                }
                catch (Exception) { }
            }
            try
            {
                var mailmessage = new MimeMessage();
                mailmessage.From.Add(new MailboxAddress(smtpfromUser, smtpfromEmail));
                mailmessage.To.Add(new MailboxAddress(smtptoUser, smtptoEmail));
                mailmessage.Subject = subject;

                if (smtpAttachment)
                {
                    var body = new TextPart("plain")
                    {
                        Text = "Message is in attachment"
                    };
                    var attachment = new MimePart("text", "plain")
                    {
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        FileName = Path.GetFileName(smtpattachname)
                    };

                    byte[] byteArray = Encoding.ASCII.GetBytes(message);
                    MemoryStream stream = new MemoryStream(byteArray);
                    attachment.Content = new MimeContent(stream, ContentEncoding.Default);

                    var multipart = new Multipart("mixed") {
                        body,
                        attachment
                    };

                    // now set the multipart/mixed as the message body
                    mailmessage.Body = multipart;
                }
                else
                {
                    mailmessage.Body = new TextPart("plain")
                    {
                        Text = message
                    };
                }

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect(smtphost, smtpport, smtpuseSSL);
                    //client.Connect("smtp.mail.yahoo.com", 465, true);

                    // Note: only needed if the SMTP server requires authentication
                    if (smtpuser != null)
                    {
                        client.Authenticate(smtpuser, smtppass);
                    }
                    //                  client.Authenticate("dave_on_tour@yahoo.com", "!@aiw2dihsf!@");

                    client.Send(mailmessage);
                    client.Disconnect(true);
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"SMTP Client Send Exception: {e.Message}");
                return false;
            }
        }
    }
}