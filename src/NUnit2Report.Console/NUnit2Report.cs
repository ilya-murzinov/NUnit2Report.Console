// -----------------------------------------------------------------------
// <copyright file="NUnit2Report.cs" company="Juan Pablo Olmos Lara (Jupaol)">
//
// NUnit2ReportTask.cs
// 
// Author:
//    Gilles Bayon (gilles.bayon@laposte.net)
// Updated Author:
//    Thang Chung (email: thangchung@ymail.com, website: weblogs.asp.net/thangchung)
//    Juan Pablo Olmos (email: jupaol@hotmail.com, website: http://jupaol.blogspot.com/)
// 
// Copyright (C) 2010 ThangChung
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 
// </copyright>
// -----------------------------------------------------------------------

namespace NUnit2Report.Console
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.XPath;
    using System.Xml.Xsl;
    using CuttingEdge.Conditions;

    /// <summary>
    /// Creates NUnit reports using XSL files. The reports can be created using Frames or without frames
    /// </summary>
    public class NUnit2Report
    {
        /// <summary>
        /// Default output file name to be used if <see cref="OutputFilename"/> is nto specified
        /// </summary>
        private const string DefaultOutputFileName = "index.htm";

        /// <summary>
        /// Default directory name to be used if <see cref="OutputDirectory"/> is not specified
        /// </summary>
        private const string DefaultOutputDirectoryName = ".\\DefaultReport";

        /// <summary>
        /// Xsl NoFrame definition file name
        /// </summary>
        private const string XslDefinitionFileName = "ReportTemplate.xsl";

        /// <summary>
        /// XSL Globalization definition file name
        /// </summary>
        /// <remarks>
        /// Used to load the translations from the "Traductions.xml"
        /// </remarks>
        private const string XslGlobalizationDefinitionFileName = "i18n.xsl";

        /// <summary>
        /// Represents the XML summary document to be used in all transformations
        /// </summary>
        private XmlDocument xmlSummaryDocument;

        /// <summary>
        /// Represents the XSL Globalization definitions file path
        /// </summary>
        private string xslGlobalizationDefinitionFilePath;

        /// <summary>
        /// Rrepresents the common XSLT arguments sued in all transformations
        /// </summary>
        private XsltArgumentList commonXsltArguments;

        /// <summary>
        /// Represents the tool path - the current executing assembly path
        /// </summary>
        private string toolPath;

        /// <summary>
        /// Rrepresents the Xsl NoFrames definition file path (full path)
        /// </summary>
        private string xslDefinitionFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnit2Report"/> class.
        /// </summary>
        /// <param name="xmlNUnitResultFiles">The XML N unit result files.</param>
        public NUnit2Report(IEnumerable<string> xmlNUnitResultFiles)
        {
            Condition.Requires(xmlNUnitResultFiles).IsNotNull().IsNotEmpty();

            this.Language = ReportLanguage.English;
            this.XmlNUnitResultFiles = xmlNUnitResultFiles;
            this.OpenDescription = false;

            this.toolPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            this.xslGlobalizationDefinitionFilePath = Path.Combine(this.toolPath, "xsl\\" + XslGlobalizationDefinitionFileName);
        }

        /// <summary>
        /// Gets or sets the output language.
        /// </summary>
        public ReportLanguage Language { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the description in the transformed html files will be opened.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the description in the transformed html files will be opened; otherwise, <c>false</c>.
        /// </value>
        public bool OpenDescription { get; set; }

        /// <summary>
        /// Gets or sets the directory where the files resulting from the transformation should be written to.
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets the index of the Output HTML file(s).
        /// Default to "index.htm".
        /// </summary>
        public string OutputFilename { get; set; }

        /// <summary>
        /// Gets the NUnit XML result files to use as input
        /// </summary>
        public IEnumerable<string> XmlNUnitResultFiles { get; private set; }

        /// <summary>
        /// This is where the work is done
        /// </summary>
        public void Execute()
        {
            this.ConfigureOptionslSettings();
            this.CreateOutputDirectory();

            this.commonXsltArguments = this.GetCommonXsltProperties();
            this.xmlSummaryDocument = this.CreateSummaryXmlDoc();
            this.CreateReport();
        }

        /// <summary>
        /// Configures the optional settings.
        /// </summary>
        private void ConfigureOptionslSettings()
        {
            this.OutputFilename = string.IsNullOrWhiteSpace(this.OutputFilename) ? DefaultOutputFileName : this.OutputFilename;
            this.OutputDirectory = string.IsNullOrWhiteSpace(this.OutputDirectory) ? DefaultOutputDirectoryName : this.OutputDirectory;
            this.xslDefinitionFilePath = Path.Combine(this.toolPath, "xsl\\" + XslDefinitionFileName);
        }

        /// <summary>
        /// Creates the output directory.
        /// </summary>
        private void CreateOutputDirectory()
        {
            if (!Directory.Exists(this.OutputDirectory))
            {
                Directory.CreateDirectory(this.OutputDirectory);
            }
        }

        /// <summary>
        /// Initializes the XmlDocument instance
        /// used to summarize the test results
        /// </summary>
        /// <returns>
        /// The Xml representing the Sumamry to be used in all transformations
        /// </returns>
        private XmlDocument CreateSummaryXmlDoc()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("testsummary");

            root.SetAttribute("created", DateTime.Now.ToString());
            doc.AppendChild(root);

            foreach (var file in this.XmlNUnitResultFiles)
            {
                var source = new XmlDocument();

                source.Load(file);
                if (source.DocumentElement == null)
                {
                    continue;
                }

                var node = doc.ImportNode(source.DocumentElement, true);

                if (doc.DocumentElement != null)
                {
                    doc.DocumentElement.AppendChild(node);
                }
            }

            return doc;
        }

        /// <summary>
        /// Builds an XsltArgumentList with all
        /// the properties defined in the
        /// current project as XSLT parameters.
        /// </summary>
        /// <returns>
        /// The <c>XsltArgumentList</c> containing the common Xslt arguments
        /// </returns>
        private XsltArgumentList GetCommonXsltProperties()
        {
            var args = new XsltArgumentList();

            args.AddParam("sys.os", string.Empty, Environment.OSVersion.ToString());
            args.AddParam("sys.clr.version", string.Empty, Environment.Version.ToString());
            args.AddParam("sys.machine.name", string.Empty, Environment.MachineName);
            args.AddParam("sys.username", string.Empty, Environment.UserName);

            // Add argument to the C# XML comment file
            args.AddParam("summary.xml", string.Empty, string.Empty);

            // Add open.description argument
            args.AddParam("open.description", string.Empty, this.OpenDescription ? "yes" : "no");

            return args;
        }

        /// <summary>
        /// Creates the no frames report.
        /// </summary>
        private void CreateReport()
        {
            var xslTransform = new XslCompiledTransform();

            xslTransform.Load(this.xslDefinitionFilePath, new XsltSettings(true, true), new XmlUrlResolver());

            // tmpFirstTransformPath hold the first transformation
            var tmpFirstTransformPath = Path.GetTempFileName();
            var firstTransformationStream = new XmlTextWriter(tmpFirstTransformPath, System.Text.ASCIIEncoding.UTF8);
            xslTransform.Transform(new XmlNodeReader(this.xmlSummaryDocument), this.commonXsltArguments, firstTransformationStream);
            firstTransformationStream.Flush();
            firstTransformationStream.Close();

            // ---------- i18n --------------------------
            var xsltI18NArgs = new XsltArgumentList();
            xsltI18NArgs.AddParam("lang", string.Empty, this.Language.GetLanguageString());

            var xslt = new XslCompiledTransform();

            xslt.Load(this.xslGlobalizationDefinitionFilePath, new XsltSettings(true, true), new XmlUrlResolver());

            var xmlDoc = new XPathDocument(tmpFirstTransformPath);
            var writerFinal = new XmlTextWriter(Path.Combine(this.OutputDirectory, this.OutputFilename), System.Text.Encoding.GetEncoding("utf-8"));

            // Apply the second transform to xmlReader to final ouput
            xslt.Transform(xmlDoc, xsltI18NArgs, writerFinal);
            writerFinal.Close();
        }

        /// <summary>
        /// Writes the specified stream containing the Xslt fragment of code.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="fileName">Name of the file.</param>
        private void Write(TextReader stream, string fileName)
        {
            // Load the XmlTextReader from the stream
            var reader = new XmlTextReader(stream);
            var xslTransform = new XslCompiledTransform();

            // Load the stylesheet from the stream.
            xslTransform.Load(reader, new XsltSettings(true, true), new XmlUrlResolver());

            // xmlDoc = new XPathDocument("result.xml");

            // tmpFirstTransformPath hold the first transformation
            var tmpFirstTransformPath = Path.GetTempFileName();
            var firstTransformationStream = new XmlTextWriter(tmpFirstTransformPath, System.Text.ASCIIEncoding.UTF8);

            xslTransform.Transform(new XmlNodeReader(this.xmlSummaryDocument), this.commonXsltArguments, firstTransformationStream);
            firstTransformationStream.Flush();
            firstTransformationStream.Close();

            if (fileName.EndsWith(".css"))
            {
                File.Copy(tmpFirstTransformPath, fileName, true);
                return;
            }

            // ---------- i18n --------------------------
            var xsltI18NArgs = new XsltArgumentList();

            xsltI18NArgs.AddParam("lang", string.Empty, this.Language.GetLanguageString());

            var xslt = new XslCompiledTransform();

            // Load the stylesheet.
            xslt.Load(this.xslGlobalizationDefinitionFilePath, new XsltSettings(true, true), new XmlUrlResolver());

            var xmlDoc = new XPathDocument(tmpFirstTransformPath);
            var writerFinal = new XmlTextWriter(fileName, System.Text.Encoding.GetEncoding("utf-8"));

            // Apply the second transform to xmlReader to final ouput
            xslt.Transform(xmlDoc, xsltI18NArgs, writerFinal);
            writerFinal.Close();
        }
    } // class NUnit2ReportTask
} // namespace  NAnt.NUnit2ReportTasks