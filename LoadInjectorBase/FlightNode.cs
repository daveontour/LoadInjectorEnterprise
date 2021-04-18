using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjectorBase {

    // Class for holding the flight information that is contained in the Towing message
    public class FlightNode {
        public string nature;
        public string airlineCode;
        public string airlineCodeICAO;
        public string fltNumber;
        public string schedDate;
        public string schedTime;
        public string arrivalAirport;
        public string departureAirport;
        public string arrivalAirportICAO;
        public string departureAirportICAO;

        private XmlNode node;
        private readonly XmlNamespaceManager nsmgr;
        public DateTime dateTime;
        public Dictionary<string, string> externals = new Dictionary<string, string>();
        public List<string> propertyList;
        public string rawFlight;

        public string DisplayFlight => airlineCode + fltNumber;

        public string FightXML => node.OuterXml;

        public FlightNode() {
        }

        public void UpdateFlight(FlightNode u) {
            nature = u.nature;
            airlineCode = u.airlineCode;
            airlineCodeICAO = u.airlineCodeICAO;
            fltNumber = u.fltNumber;
            schedDate = u.schedDate;
            schedTime = u.schedTime;
            arrivalAirport = u.arrivalAirport;
            departureAirport = u.departureAirport;
            arrivalAirportICAO = u.arrivalAirportICAO;
            departureAirportICAO = u.departureAirportICAO;
            node = u.node;

            dateTime = u.dateTime;
            externals = u.externals;
            propertyList = u.propertyList;
            rawFlight = u.rawFlight;
        }

        // The "node" parameter is one the XElement of the "FlightIndentifier" element of the Towing message
        public FlightNode(XmlNode node, XmlNamespaceManager nsmgr) {
            rawFlight = node.OuterXml;

            nature = node.SelectSingleNode(".//ams:FlightId/ams:FlightKind", nsmgr)?.InnerText;

            try {
                airlineCode = node.SelectSingleNode(".//ams:FlightId/ams:AirlineDesignator[@codeContext='IATA']", nsmgr)?.InnerText;
            } catch (Exception) {
                airlineCode = null;
            }
            try {
                airlineCodeICAO = node.SelectSingleNode(".//ams:FlightId/ams:AirlineDesignator[@codeContext='ICAO']", nsmgr)?.InnerText;
            } catch (Exception) {
                airlineCodeICAO = null;
            }

            fltNumber = node.SelectSingleNode(".//ams:FlightId/ams:FlightNumber", nsmgr)?.InnerText;
            schedDate = node.SelectSingleNode(".//ams:FlightId/ams:ScheduledDate", nsmgr)?.InnerText;
            schedTime = node.SelectSingleNode(".//ams:FlightState/ams:ScheduledTime", nsmgr)?.InnerText;

            dateTime = DateTime.Parse(schedTime);

            XmlNodeList values = node.SelectNodes(".//ams:FlightState/ams:Value", nsmgr);
            foreach (XmlNode x in values) {
                externals.Add(x.Attributes["propertyName"].Value, x.InnerText);
            }

            if (nature == "Arrival") {
                arrivalAirport = node.SelectSingleNode(".//ams:FlightId/ams:AirportCode[@codeContext='IATA']", nsmgr)?.InnerText;
                try {
                    departureAirport = node.SelectSingleNode(".//ams:FlightState/ams:Route/ams:ViaPoints/ams:RouteViaPoint[@sequenceNumber='0']/ams:AirportCode[@codeContext='IATA']", nsmgr)?.InnerText;
                } catch (Exception) {
                    departureAirport = null;
                }

                arrivalAirportICAO = node.SelectSingleNode(".//ams:FlightId/ams:AirportCode[@codeContext='ICAO']", nsmgr)?.InnerText;
                try {
                    departureAirportICAO = node.SelectSingleNode(".//ams:FlightState/ams:Route/ams:ViaPoints/ams:RouteViaPoint[@sequenceNumber='0']/ams:AirportCode[@codeContext='ICAO']", nsmgr)?.InnerText;
                } catch (Exception) {
                    departureAirportICAO = null;
                }
            } else {
                departureAirport = node.SelectSingleNode(".//ams:FlightId/ams:AirportCode[@codeContext='IATA']", nsmgr)?.InnerText;
                try {
                    arrivalAirport = node.SelectSingleNode(".//ams:FlightState/ams:Route/ams:ViaPoints/ams:RouteViaPoint[@sequenceNumber='0']/ams:AirportCode[@codeContext='IATA']", nsmgr)?.InnerText;
                } catch (Exception) {
                    arrivalAirport = null;
                }

                departureAirportICAO = node.SelectSingleNode(".//ams:FlightId/ams:AirportCode[@codeContext='ICAO']", nsmgr)?.InnerText;
                try {
                    arrivalAirportICAO = node.SelectSingleNode(".//ams:FlightState/ams:Route/ams:ViaPoints/ams:RouteViaPoint[@sequenceNumber='0']/ams:AirportCode[@codeContext='ICAO']", nsmgr)?.InnerText;
                } catch (Exception) {
                    arrivalAirportICAO = null;
                }
            }

            propertyList = new List<string>(externals.Keys);
            propertyList.Sort();

            this.node = node;
            this.nsmgr = nsmgr;
        }

        public string GetXPath(string xpath) {
            try {
                return node.SelectSingleNode(xpath, nsmgr)?.InnerText;
            } catch (Exception) {
                return null;
            }
        }

        public new string ToString() {
            return $"AirlineCode: {airlineCode}, Flight Number: {fltNumber}, Nature: {nature}, Scheuled Date: {schedDate}, Scheuled Time: {schedTime}, Arrival Airport {arrivalAirport}, Departure Airport {departureAirport}";
        }

        private readonly string getFlightTemplate = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ams6=""http://www.sita.aero/ams6-xml-api-webservice"" xmlns:wor=""http://schemas.datacontract.org/2004/07/WorkBridge.Modules.AMS.AMSIntegrationAPI.Mod.Intf.DataTypes"">
   <soapenv:Header/>
   <soapenv:Body>
      <ams6:GetFlight>
         <!--Optional:-->
         <ams6:sessionToken>@token</ams6:sessionToken>
         <!--Optional:-->
         <ams6:flightId>
            <wor:_hasAirportCodes>false</wor:_hasAirportCodes>
            <wor:_hasFlightDesignator>true</wor:_hasFlightDesignator>
            <wor:_hasScheduledTime>false</wor:_hasScheduledTime>
            <wor:airlineDesignatorField>
               <!--Zero or more repetitions:-->
               <wor:LookupCode>
                  <wor:codeContextField>IATA</wor:codeContextField>
                  <wor:valueField>@airline</wor:valueField>
               </wor:LookupCode>
            </wor:airlineDesignatorField>
            <wor:airportCodeField>
               <!--Zero or more repetitions:-->
               <wor:LookupCode>
                  <wor:codeContextField>IATA</wor:codeContextField>
                  <wor:valueField>@airport</wor:valueField>
               </wor:LookupCode>
            </wor:airportCodeField>
            <wor:flightKindField>@kind</wor:flightKindField>
            <wor:flightNumberField>@fltNum</wor:flightNumberField>
            <wor:scheduledDateField>@schedDate</wor:scheduledDateField>
         </ams6:flightId>
      </ams6:GetFlight>
   </soapenv:Body>
</soapenv:Envelope>";

        public async Task<FlightNode> RefeshFlight(string amshost, string token, string apt_code) {
            try {
                Console.WriteLine($"Refreshing Flight {node}");
                string flightsQuery = getFlightTemplate
                    .Replace("@token", token)
                    .Replace("@airport", apt_code)
                    .Replace("@airline", airlineCode)
                    .Replace("@schedDate", schedDate)
                    .Replace("@kind", nature)
                    .Replace("@fltNum", fltNumber);

                using (var client = new HttpClient()) {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, amshost) {
                        Content = new StringContent(flightsQuery, Encoding.UTF8, "text/xml")
                    };
                    requestMessage.Headers.Add("SOAPAction", "http://www.sita.aero/ams6-xml-api-webservice/IAMSIntegrationService/GetFlight");

                    using (HttpResponseMessage response = await client.SendAsync(requestMessage)) {
                        if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent) {
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(await response.Content.ReadAsStringAsync());
                            XmlElement flightsElement = doc.DocumentElement;

                            XmlNamespaceManager nsmgr = new XmlNamespaceManager(flightsElement.OwnerDocument.NameTable);
                            nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");

                            XmlNode fl = flightsElement.SelectSingleNode("//ams:Flight", nsmgr);
                            FlightNode fn = new FlightNode(fl, nsmgr);

                            Console.WriteLine("Refreshed Flight");

                            return fn;
                        } else {
                            Console.WriteLine("Unable to Refresh Flight - Using existing data");
                            return this;
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Unable to Refresh Flight - Using existing data.  {ex.Message}");
            }

            return this;
        }
    }
}