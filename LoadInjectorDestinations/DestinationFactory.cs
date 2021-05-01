using LoadInjectorBase;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;

namespace LoadInjectorDestinations {

    public class DestinationFactory {

        public SenderAbstract GetSender(string senderName) {
            string senderClassName = GetSenderClassName(senderName);

            var type = typeof(SenderAbstract);
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(s => s.GetTypes())
                            .Where(p => type.IsAssignableFrom(p));
            foreach (Type t in types) {
                if (!t.IsAbstract && !t.IsInterface) {
                    if (t.Name == senderClassName) {
                        SenderAbstract dest = (SenderAbstract)Activator.CreateInstance(t);
                        return dest;
                    }
                }
            }
            return null;
        }

        private string GetSenderClassName(string senderName) {
            try {
                using (TextFieldParser parser = new TextFieldParser("destination.mapping")) {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(":");

                    while (!parser.EndOfData) {
                        try {
                            string[] fields = parser.ReadFields();
                            if (fields[0] == senderName) {
                                return fields[1];
                            }
                        } catch {
                            // Do nothing, try the next line
                        }
                    }

                    return null;
                }
            } catch (Exception ex) {
                Debug.WriteLine("Error reading CSV Lookup File destination.mapping " + ex.Message);
                return null;
            }
        }
    }
}