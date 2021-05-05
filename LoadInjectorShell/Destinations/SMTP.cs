//using LoadInjector.Common;
//using LoadInjector.RunTime;
//using MailKit.Net.Smtp;
//using MimeKit;
//using NLog;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.IO;
//using System.Text;
//using System.Xml;
//using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {
    //class Smtp : IDestinationType {
    //    public string name = "SMTP";
    //    public string description = "SMTP";
    //    public string ProtocolName { get { return name; } }
    //    public string ProtocolDescription { get { return description; } }
    //    public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
    //        return new SmtpPropertyGrid(dataModel, view);
    //    }

    //    public DestinationAbstract GetDestinationSender() {
    //        return new DestinationSmtp();
    //    }
    //}

    //[RefreshProperties(RefreshProperties.All)]
    //[DisplayName("SMTP MQ Protocol Configuration")]
    //public class SmtpPropertyGrid : LoadInjectorGridBase {
    //    public SmtpPropertyGrid(XmlNode dataModel, IView view) {
    //        this._node = dataModel;
    //        this.view = view;
    //    }

    //    [DisplayName("SMTP Host"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The SMTP Host")]
    //    public string SMTPHost {
    //        get { return GetAttribute("smtphost"); }
    //        set { SetAttribute("smtphost", value); }
    //    }
    //    [DisplayName("SMTP Host Port"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("The port on the SMTP Host to connect to")]
    //    public string SMTPPort {
    //        get { return GetAttribute("smtpport"); }
    //        set { SetAttribute("smtpport", value); }
    //    }
    //    [DisplayName("SMTP User"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The user name if authentication is required by the SMTP Host")]
    //    public string SMTPUser {
    //        get { return GetAttribute("smtpuser"); }
    //        set { SetAttribute("smtpuser", value); }
    //    }
    //    [DisplayName("SMTP Password"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The user password if authentication is required by the SMTP Host")]
    //    public string SMTPPass {
    //        get { return GetAttribute("smtppass"); }
    //        set { SetAttribute("smtppass", value); }
    //    }
    //    [DisplayName("SMTP Use SSL"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("Use SSL connection. (Probably needs to be set)")]
    //    public bool SMTPSSL {
    //        get { return GetBoolDefaultFalseAttribute(_node, "smtpuseSSL"); }
    //        set { SetAttribute("smtpuseSSL", value); }
    //    }
    //    [DisplayName("Send As Attachment"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("Send the payload as an attachment. By default it is sent as the body of the message")]
    //    public bool SMTPAttachment {
    //        get { return GetBoolDefaultFalseAttribute(_node, "smtpAttachment"); }
    //        set { SetAttribute("smtpAttachment", value); }
    //    }

    //    [DisplayName("SMTP From User"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The name of the user sending the email")]
    //    public string SMTPFromUser {
    //        get { return GetAttribute("smtpfromUser"); }
    //        set { SetAttribute("smtpfromUser", value); }
    //    }
    //    [DisplayName("SMTP From Email"), ReadOnly(false), Browsable(true), PropertyOrder(8), DescriptionAttribute("The email address of the sending user (May be required, for example, the Yahoo SMTP Server requires this to send)")]
    //    public string SMTPFromEmail {
    //        get { return GetAttribute("smtpfromEmail"); }
    //        set { SetAttribute("smtpfromEmail", value); }
    //    }
    //    [DisplayName("SMTP To Email"), ReadOnly(false), Browsable(true), PropertyOrder(9), DescriptionAttribute("The Email address to send the message to")]
    //    public string SMTPToEmail {
    //        get { return GetAttribute("smtptoEmail"); }
    //        set { SetAttribute("smtptoEmail", value); }
    //    }

    //    [DisplayName("SMTP Subject (tokenizeable)"), ReadOnly(false), Browsable(true), PropertyOrder(10), DescriptionAttribute("The Subject of the Email. Token can be used for variable substitution")]
    //    public string SMTPSubj {
    //        get { return GetAttribute("smtpsubject"); }
    //        set { SetAttribute("smtpsubject", value); }
    //    }
    //    [DisplayName("Attachment Name (tokenizeable)"), ReadOnly(false), Browsable(true), PropertyOrder(11), DescriptionAttribute("The filename of the attachment. Token can be used for variable substitution")]
    //    public string SMTPAttachmentName {
    //        get { return GetAttribute("smtpattachname"); }
    //        set { SetAttribute("smtpattachname", value); }
    //    }
    //    [DisplayName("SMTP To User"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("The name of the 'To:' recipient")]
    //    public string SMTPToUser {
    //        get { return GetAttribute("smtptouser"); }
    //        set { SetAttribute("smtptouser", value); }
    //    }
    //}

    //class DestinationSmtp : DestinationAbstract {
    //    private string smtphost;
    //    private int smtpport;
    //    private string smtpuser;
    //    private string smtppass;
    //    private bool smtpuseSSL = false;
    //    private bool smtpAttachment = false;
    //    private string smtpsubject;
    //    private string smtpfromUser;
    //    private string smtpfromEmail;
    //    private string smtptoUser;
    //    private string smtptoEmail;
    //    private string smtpattachname;

    //    public override bool Configure(XmlNode definition, IDestinationEndPointController controller, Logger logger) {
    //        base.Configure(defn, controller, logger);

    //        try {
    //            smtphost = definition.Attributes["smtphost"].Value;
    //        } catch (Exception) {
    //            Console.WriteLine($"SMTP not defined for {definition.Attributes["name"].Value}");
    //            return false;
    //        }

    //        try {
    //            smtpport = int.Parse(definition.Attributes["smtpport"].Value);
    //        } catch (Exception) {
    //            Console.WriteLine($"No SMTP Port correctly defined for {definition.Attributes["name"].Value}");
    //            return false;
    //        }

    //        try {
    //            smtpuser = definition.Attributes["smtpuser"].Value;
    //        } catch (Exception) {
    //            smtpuser = null;
    //        }

    //        try {
    //            smtpattachname = definition.Attributes["smtpattachname"].Value;
    //        } catch (Exception) {
    //            smtpattachname = "LoadInjectorMsg";
    //        }

    //        try {
    //            smtppass = definition.Attributes["smtppass"].Value;
    //        } catch (Exception) {
    //            smtppass = null;
    //        }

    //        try {
    //            smtpsubject = definition.Attributes["smtpsubject"].Value;
    //        } catch (Exception) {
    //            smtpsubject = "Load Injector Message";
    //        }

    //        try {
    //            smtpfromUser = definition.Attributes["smtpfromUser"].Value;
    //        } catch (Exception) {
    //            smtpfromUser = "Load Injector";
    //        }

    //        try {
    //            smtpfromEmail = definition.Attributes["smtpfromEmail"].Value;
    //        } catch (Exception) {
    //            smtpfromEmail = "";
    //        }

    //        try {
    //            smtptoUser = definition.Attributes["smtptoUser"].Value;
    //        } catch (Exception) {
    //            smtptoUser = "Load Injector Recipient";
    //        }

    //        try {
    //            smtptoEmail = definition.Attributes["smtptoEmail"].Value;
    //        } catch (Exception) {
    //            Console.WriteLine($"No SMTP Destination Email defined for {definition.Attributes["name"].Value}");
    //            return false;
    //        }

    //        try {
    //            smtpuseSSL = bool.Parse(definition.Attributes["smtpuseSSL"].Value);
    //        } catch (Exception) {
    //            smtpuseSSL = false;
    //        }

    //        try {
    //            smtpAttachment = bool.Parse(definition.Attributes["smtpAttachment"].Value);
    //        } catch (Exception) {
    //            smtpAttachment = false;
    //        }

    //        return true;

    //    }

    //    public override void Send(String message, List<Variable> vars) {
    //        string subject = smtpsubject;
    //        foreach (Variable v in vars) {
    //            try {
    //                subject = subject.Replace(v.token, v.value);
    //                smtpattachname = subject.Replace(v.token, v.value);
    //            } catch (Exception) {
    //                // NO-OP
    //            }
    //        }
    //        try {
    //            var mailmessage = new MimeMessage();
    //            mailmessage.From.Add(new MailboxAddress(smtpfromUser, smtpfromEmail));
    //            mailmessage.To.Add(new MailboxAddress(smtptoUser, smtptoEmail));
    //            mailmessage.Subject = subject;

    //            if (smtpAttachment) {
    //                var body = new TextPart("plain") {
    //                    Text = "Message is in attachment"
    //                };
    //                var attachment = new MimePart("text", "plain") {
    //                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
    //                    FileName = Path.GetFileName(smtpattachname)
    //                };

    //                byte[] byteArray = Encoding.ASCII.GetBytes(message);
    //                MemoryStream stream = new MemoryStream(byteArray);
    //                attachment.Content = new MimeContent(stream, ContentEncoding.Default);

    //                var multipart = new Multipart("mixed") {
    //                    body,
    //                    attachment
    //                };

    //                // now set the multipart/mixed as the message body
    //                mailmessage.Body = multipart;
    //            } else {
    //                mailmessage.Body = new TextPart("plain") {
    //                    Text = message
    //                };
    //            }

    //            using (var client = new SmtpClient()) {
    //                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
    //                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

    //                client.Connect(smtphost, smtpport, smtpuseSSL);

    //                // Note: only needed if the SMTP server requires authentication
    //                if (smtpuser != null) {
    //                    client.Authenticate(smtpuser, smtppass);
    //                }
    //                client.Send(mailmessage);
    //                client.Disconnect(true);
    //            }
    //        } catch (Exception e) {
    //            Console.WriteLine($"SMTP Client Send Exception: {e.Message}");
    //        }
    //    }
    //}
}