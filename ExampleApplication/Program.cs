﻿using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DocXToPdfConverter;

namespace ExampleApplication
{
    class Program
    {
        static void Main(string[] args)
        {

            //Do you know the path to your word-template? Then you can omit this
            string executableLocation = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
            string xslLocation = Path.Combine(executableLocation, "Test-Template.docx");

            //Prepare texts, which you want to insert into the custom fields in the template (remember
            //to use start and stop tags.
            //Note that line breaks can be inserted as what you define them in ReplacementDictionaries.NewLineTag (here we use <br/>).

            var docxPlaceholders = new Placeholders();
            docxPlaceholders.NewLineTag = "<br/>";
            docxPlaceholders.TextPlaceholderStartTag = "##";
            docxPlaceholders.TextPlaceholderEndTag = "##";
            docxPlaceholders.TablePlaceholderStartTag = "==";
            docxPlaceholders.TablePlaceholderEndTag = "==";
            docxPlaceholders.ImagePlaceholderStartTag = "++";
            docxPlaceholders.ImagePlaceholderEndTag = "++";

            //You should be able to also use other OpenXML tags in your strings
            docxPlaceholders.TextPlaceholders = new Dictionary<string, string>
            {
                {"Name", "Mr. Miller" },
                {"Street", "89 Brook St" },
                {"City", "Brookline MA 02115<br/>USA" },
                {"InvoiceNo", "5" },
                {"Total", "U$ 4,500" },
                {"Date", "28 Jul 2019" }
            };



            //Table ROW replacements are a little bit more complicated: With them you can
            //fill out only one table row in a table and it will add as many rows as you 
            //need, depending on the string Array.
            docxPlaceholders.TablePlaceholders = new List<Dictionary<string, string[]>>
            {
                
                    new Dictionary<string, string[]>()
                    {
                        {"Name", new string[]{ "Homer Simpson", "Mr. Burns", "Mr. Smithers" }},
                        {"Department", new string[]{ "Power Plant", "Administration", "Administration" }},
                        {"Responsibility", new string[]{ "Oversight", "CEO", "Assistant" }},
                        {"Telephone number", new string[]{ "888-234-2353", "888-295-8383", "888-848-2803" }}
                    },
                    new Dictionary<string, string[]>()
                    {
                        {"Qty", new string[]{ "2", "5", "7" }},
                        {"Product", new string[]{ "Software development", "Customization", "Travel expenses" }},
                        {"Price", new string[]{ "U$ 2,000", "U$ 1,000", "U$ 1,500" }},
                    }

            };


            var htmlPlaceholders = new Placeholders();
            htmlPlaceholders.NewLineTag = "<br/>";
            htmlPlaceholders.TextPlaceholderStartTag = "##";
            htmlPlaceholders.TextPlaceholderEndTag = "##";
            htmlPlaceholders.TablePlaceholderStartTag = "==";
            htmlPlaceholders.TablePlaceholderEndTag = "==";
            htmlPlaceholders.ImagePlaceholderStartTag = "++";
            htmlPlaceholders.ImagePlaceholderEndTag = "++";

            //You should be able to also use other OpenXML tags in your strings
            htmlPlaceholders.TextPlaceholders = new Dictionary<string, string>
            {
                {"Name", "Mr. Miller" },
                {"Street", "89 Brook St" },
                {"City", "Brookline MA 02115<br/>USA" },
                {"InvoiceNo", "5" },
                {"Total", "U$ 4,500" },
                {"Date", "28 Jul 2019" }
            };



            //Table ROW replacements are a little bit more complicated: With them you can
            //fill out only one table row in a table and it will add as many rows as you 
            //need, depending on the string Array.
            htmlPlaceholders.TablePlaceholders = new List<Dictionary<string, string[]>>
            {

                    new Dictionary<string, string[]>()
                    {
                        {"Name", new string[]{ "Homer Simpson", "Mr. Burns", "Mr. Smithers" }},
                        {"Department", new string[]{ "Power Plant", "Administration", "Administration" }},
                        {"Responsibility", new string[]{ "Oversight", "CEO", "Assistant" }},
                        {"Telephone number", new string[]{ "888-234-2353", "888-295-8383", "888-848-2803" }}
                    },
                    new Dictionary<string, string[]>()
                    {
                        {"Qty", new string[]{ "2", "5", "7" }},
                        {"Product", new string[]{ "Software development", "Customization", "Travel expenses" }},
                        {"Price", new string[]{ "U$ 2,000", "U$ 1,000", "U$ 1,500" }},
                    }

            };


            //You have to add the images as a memory stream to the Dictionary! Place a key (placeholder) into the docx template.
            //There is a method to read files as memory streams (GetFileAsMemoryStream)
            //We already did that with <+++>ProductImage<+++>

            var productImage =
                StreamHandler.GetFileAsMemoryStream(Path.Combine(executableLocation, "ProductImage.jpg"));

            var qrImage =
                StreamHandler.GetFileAsMemoryStream(Path.Combine(executableLocation, "QRCode.PNG"));

            htmlPlaceholders.ImagePlaceholders = new Dictionary<string, MemoryStream>
            {
                {"QRCode", qrImage },
                {"ProductImage", productImage }
            };

            //var doc = new DocXHandler(xslLocation, myDictionary);
            //var docxStream = doc.ReplaceAll();
            //var docxStream = doc.CleanDocument();
            //StreamHandler.WriteMemoryStreamToDisk(docxStream, "F:\\vmc\\out.docx");
            var test = new ReportGenerator(@"F:\PortableApps\LibreOfficePortable\App\libreoffice\program\soffice.exe");
            //test.GenerateReportFromHtmlToPdf("f:\\vmc\\simhtml.htm", "f:\\vmc\\simhtml.pdf", myDictionary);
            //test.GenerateReportFromDocxToDocX("Test-Template.docx", "F:\\vmc\\template.docx", myDictionary);
            
            test.GenerateReportFromHtmlToHtml();
            

            //test.GenerateReportFromDocxToPdf("Test-Template.docx", "F:\\vmc\\template.pdf", myDictionary);
        }
    }
}
