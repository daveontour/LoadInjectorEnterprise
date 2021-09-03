using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LoadInjectorBase;

namespace LoadInjector.RunTime.EngineComponents
{
    public class ProcessorBase
    {
        protected async Task<string> GetDocumentFromPostSource(string url, XmlNode node)
        {
            try
            {
                string body = "request bofy";
                List<SourceVariable> vars = new List<SourceVariable>();
                foreach (XmlNode variableConfig in node.SelectNodes(".//variable"))
                {
                    SourceVariable var = new SourceVariable(variableConfig);
                    if (!var.ConfigOK)
                    {
                        Console.WriteLine($"Error adding source variable");
                    }
                    else
                    {
                        vars.Add(var);
                    }
                }
                foreach (SourceVariable v in vars)
                {
                    try
                    {
                        url = url.Replace(v.token, v.GetValue());
                        body = body.Replace(v.token, v.GetValue());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Variable Substitution error {ex.Message}");
                    }
                }

                var data = new StringContent(body, Encoding.UTF8, "application/text");
                try
                {
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            foreach (XmlNode header in node.SelectNodes(".//header"))
                            {
                                string key = header.Attributes["name"]?.Value;
                                string value = header.InnerText;
                                client.DefaultRequestHeaders.Add(key, value);
                            }
                            var response = await client.PostAsync(url, data);
                            string result = response.Content.ReadAsStringAsync().Result;
                            return result;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error retrieving document: {ex.Message}");
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving Document: {ex.Message}");
                    return null;
                }
            }
            catch (Exception)
            {
                // NO-OP
            }

            return null;
        }

        protected string GetDocumentFromGetSource(string url, XmlNode node)
        {
            try
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            foreach (XmlNode header in node.SelectNodes(".//header"))
                            {
                                string key = header.Attributes["name"]?.Value;
                                string value = header.InnerText;
                                client.DefaultRequestHeaders.Add(key, value);
                            }
                            string res = client.GetStringAsync(url).Result;
                            return res;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error retrieving CSV Document: {ex.Message}");
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving CSV Document: {ex.Message}");
                    return null;
                }
            }
            catch (Exception)
            {
                // NO-OP
            }

            return null;
        }
    }
}