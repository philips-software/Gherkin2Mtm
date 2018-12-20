using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Gherkin2MtmApi.Helpers;
using Gherkin2MtmApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Gherkin2Mtm.Helpers
{
    internal static class FieldMapperHelper
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(FieldMapperHelper));

        public static IList<TestCaseField> GetFieldsList()
        {
            var path = new Uri(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ??
                               throw new InvalidOperationException()).LocalPath;
            ValidateFieldMapper(path);

            var serializer = new XmlSerializer(typeof(List<TestCaseField>), new XmlRootAttribute("Fields"));
            serializer.UnknownNode += SerializerUnknownNode;
            serializer.UnknownAttribute += SerializerUnknownAttribute;

            var reader = new StreamReader(GetFieldMapperPath(path));
            var fields = (IList<TestCaseField>)serializer.Deserialize(reader);
            reader.Close();
            return fields;
        }

        private static void ValidateFieldMapper(string path)
        {
            var schema = new XmlSchemaSet();
            schema.Add("", path + @"\FieldMapper.xsd");
            var rd = XmlReader.Create(GetFieldMapperPath(path));
            var doc = XDocument.Load(rd);
            rd.Close();
            doc.Validate(schema, ValidationEventHandler);
        }

        private static string GetFieldMapperPath(string path)
        {
            return $@"{path}\FieldMapper.xml";
        }

        private static void ValidationEventHandler(object sender, ValidationEventArgs eventArgs)
        {
            if (eventArgs.Severity == XmlSeverityType.Error)
            {
                throw new FieldDefinitionNotExistException(eventArgs.Message);
            }
        }

        private static void SerializerUnknownNode(object sender, XmlNodeEventArgs e)
        {
            Logger.Error($"Unknown Node:{e.Name}\t{e.Text}");
        }

        private static void SerializerUnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            var attr = e.Attr;
            Logger.Error($"Unknown attribute, {attr.Name}={attr.Value}");
        }
    }
}
