﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OpenXmlPowerTools;
using A = DocumentFormat.OpenXml.Drawing;
using Break = DocumentFormat.OpenXml.Wordprocessing.Break;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using TableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;


namespace DocXToPdfConverter.DocXToPdfHandlers
{
    public class DocXHandler

    {
        private MemoryStream _docxMs;
        private Placeholders _rep;
        private int _imageCounter;

        public DocXHandler(string docXTemplateFilename, Placeholders rep)
        {
            _docxMs = StreamHandler.GetFileAsMemoryStream(docXTemplateFilename);
            _rep = rep;
            
        }


        public MemoryStream ReplaceAll()
        {
            if (_rep.TextPlaceholders.Count > 0)
            {
                ReplaceTexts();
            }

            if (_rep.TablePlaceholders.Count > 0 && _rep.TablePlaceholders.First().Count > 0)
            {
                ReplaceTableRows();
            }
            if (_rep.ImagePlaceholders.Count > 0)
            { 
                ReplaceImages();
            }
            _docxMs.Position = 0;

            return _docxMs;
        }
       

        public MemoryStream ReplaceTexts()
        {
            if (_rep.TextPlaceholders.Count == 0 || _rep.TextPlaceholders == null)
                return null;
            using (WordprocessingDocument doc =
                WordprocessingDocument.Open(_docxMs, true))
            {
                CleanMarkup(doc);

                var document = doc.MainDocumentPart.Document;

                foreach (var text in document.Descendants<Text>()) // <<< Here
                {
                    foreach (var replace in _rep.TextPlaceholders)
                    {
                        if (text.Text.Contains(_rep.TextPlaceholderStartTag + replace.Key + _rep.TextPlaceholderEndTag))
                        {
                            if (replace.Value.Contains(_rep.NewLineTag))//If we have line breaks present
                            {
                                string[] repArray = replace.Value.Split(new string[] {_rep.NewLineTag}, StringSplitOptions.None);

                                var lastInsertedText = text;
                                var lastInsertedBreak = new Break();

                                for (var i = 0; i < repArray.Length; i++)
                                {
                                    if (i == 0)//The text is only replaced with the first part of the replacement array
                                    {
                                        text.Text = text.Text.Replace(_rep.TextPlaceholderStartTag + replace.Key + _rep.TextPlaceholderEndTag, repArray[i]);

                                    }
                                    else
                                    {
                                        var tmpText = new Text(repArray[i]);
                                        var tmpBreak = new Break();
                                        text.Parent.InsertAfter(tmpBreak, lastInsertedText);
                                        lastInsertedBreak = tmpBreak;
                                        text.Parent.InsertAfter(tmpText, lastInsertedBreak);
                                        lastInsertedText = tmpText;
                                    }

                                }

                            }
                            else
                            {
                                text.Text = text.Text.Replace(_rep.TextPlaceholderStartTag + replace.Key + _rep.TextPlaceholderEndTag, replace.Value);

                            }
                        }

                    }
                }

            }

            _docxMs.Position = 0;
            return _docxMs;
        }


        public MemoryStream ReplaceTableRows()
        {
            if (_rep.TablePlaceholders.Count == 0 || _rep.TablePlaceholders == null)
                return null;

            using (WordprocessingDocument doc =
                WordprocessingDocument.Open(_docxMs, true))
            {

                CleanMarkup(doc);

                var document = doc.MainDocumentPart.Document;

                foreach (var trDict in _rep.TablePlaceholders) //Take a Row (one Dictionary) at a time
                {
                    var trCol0 = trDict.First();
                    // Find the first text element matching the search string 
                    // where the text is inside a table cell --> this is the row we are searching for.
                    var textElement = document.Body.Descendants<Text>()
                        .FirstOrDefault(t =>
                            t.Text == _rep.TablePlaceholderStartTag + trCol0.Key + _rep.TablePlaceholderEndTag &&
                            t.Ancestors<DocumentFormat.OpenXml.Wordprocessing.TableCell>().Any());
                    if (textElement != null)
                    {
                        var newTableRows = new List<TableRow>();
                        var tableRow = textElement.Ancestors<TableRow>().First();


                        for (var j = 0; j < trCol0.Value.Length; j++) //Lets create row by row and replace placeholders
                        {
                            newTableRows.Add((TableRow)tableRow.CloneNode(true));
                            var tableRowCopy = newTableRows[newTableRows.Count - 1];

                            foreach (var text in tableRow.Descendants<Text>()
                            ) //Cycle through the cells of the row to replace from the Dictionary value ( string array)
                            {
                                for (var index = 0;
                                    index < trDict.Count;
                                    index++) //Now cycle through the "columns" (keys) of the Dictionary and replace item by item
                                {
                                    var item = trDict.ElementAt(index);

                                    if (text.Text.Contains(_rep.TablePlaceholderStartTag + item.Key +
                                                           _rep.TablePlaceholderEndTag))
                                    {
                                        if (item.Value[j].Contains(_rep.NewLineTag)) //If we have line breaks present
                                        {
                                            string[] repArray = item.Value[j].Split(new string[] {_rep.NewLineTag},
                                                StringSplitOptions.None);

                                            var lastInsertedText = text;
                                            var lastInsertedBreak = new Break();

                                            for (var i = 0; i < repArray.Length; i++)
                                            {
                                                if (i == 0
                                                ) //The text is only replaced with the first part of the replacement array
                                                {
                                                    text.Text = text.Text.Replace(
                                                        _rep.TablePlaceholderStartTag + item.Key +
                                                        _rep.TablePlaceholderEndTag, repArray[i]);

                                                }
                                                else
                                                {
                                                    var tmpText = new Text(repArray[i]);
                                                    var tmpBreak = new Break();
                                                    text.Parent.InsertAfter(tmpBreak, lastInsertedText);
                                                    lastInsertedBreak = tmpBreak;
                                                    text.Parent.InsertAfter(tmpText, lastInsertedBreak);
                                                    lastInsertedText = tmpText;
                                                }

                                            }

                                        }
                                        else
                                        {
                                            text.Text = text.Text.Replace(
                                                _rep.TablePlaceholderStartTag + item.Key + _rep.TablePlaceholderEndTag,
                                                item.Value[j]);

                                        }

                                        break;
                                    }
                                }



                            }

                            if (j < trCol0.Value.Length - 1)
                            {
                                tableRow.Parent.InsertAfter(tableRowCopy, tableRow);
                                tableRow = tableRowCopy;
                            }
                            
                        }

                    }


                }

            }
            _docxMs.Position = 0;
            return _docxMs;
            
        }

        /*
        private static RunProperties GetRunPropertyFromTableCell(TableRow rowCopy, int cellIndex)
        {
            var runProperties = new RunProperties();
            foreach (var T in rowCopy.Descendants<DocumentFormat.OpenXml.Wordprocessing.TableCell>().ElementAt(cellIndex).GetFirstChild<Paragraph>()
                .GetFirstChild<Run>().GetFirstChild<RunProperties>())
            {
                runProperties.AppendChild(T.CloneNode(true));
            }

            return runProperties;
        }
        */




        public MemoryStream ReplaceImages()
        {
            if (_rep.ImagePlaceholders.Count == 0 || _rep.ImagePlaceholders == null)
                return null;

            using (WordprocessingDocument doc =
                WordprocessingDocument.Open(_docxMs, true))
            {
                CleanMarkup(doc);

                var document = doc.MainDocumentPart.Document;

                foreach (var text in document.Descendants<Text>()) // <<< Here
                {
                    foreach (var replace in _rep.ImagePlaceholders)
                    {
                        _imageCounter++;
                        if (text.Text.Contains(_rep.ImagePlaceholderStartTag + replace.Key + _rep.ImagePlaceholderEndTag))
                        {
                            
                            text.Text = text.Text.Replace(
                                _rep.ImagePlaceholderStartTag + replace.Key + _rep.ImagePlaceholderEndTag,
                                "");
                            
                            AppendImageToElement2(replace, text, doc);

                            
                        }

                    }
                }
            }
            _docxMs.Position = 0;
            return _docxMs;

        }


        private void AppendImageToElement(KeyValuePair<string, MemoryStream> placeholder, OpenXmlElement element, WordprocessingDocument wordprocessingDocument)
        {

            MainDocumentPart mainPart = wordprocessingDocument.MainDocumentPart;

            ImagePart imagePart = mainPart.AddImagePart(ImageHandler.GetImagePartTypeFromMemStream(placeholder.Value));

            imagePart.FeedData(placeholder.Value);

            var imgTmp = ImageHandler.GetImageFromStream(placeholder.Value);

            var drawing = GetImageElement(mainPart.GetIdOfPart(imagePart), "image", "picture", imgTmp.Width, imgTmp.Height );

            //var drawing = GetImageElement(wordprocessingDocument, mainPart.GetIdOfPart(imagePart));
            element.Parent.InsertAfter(new Paragraph(new Run(drawing)), element);
            
        }


        private void AppendImageToElement2(KeyValuePair<string, MemoryStream> placeholder, OpenXmlElement element, WordprocessingDocument wordprocessingDocument)
        {

            MainDocumentPart mainPart = wordprocessingDocument.MainDocumentPart;

            Uri imageUri = new Uri("/word/media/" +
                                   placeholder.Key + _imageCounter, UriKind.Relative);

            // Create "image" part in /word/media
            // Change content type for other image types.
            PackagePart packageImagePart =
                wordprocessingDocument.Package.CreatePart(imageUri, "Image/"+ImageHandler.GetImageTypeFromMemStream(placeholder.Value));


            // Feed data.
            placeholder.Value.Position = 0;
            byte[] imageBytes = placeholder.Value.ToArray();// File.ReadAllBytes(fileName);
            packageImagePart.GetStream().Write(imageBytes, 0, imageBytes.Length);

            PackagePart documentPackagePart =
                mainPart.OpenXmlPackage.Package.GetPart(new Uri("/word/document.xml", UriKind.Relative));

            // URI to the image is relative to relationship document.
            PackageRelationship imageRelationshipPart = documentPackagePart.CreateRelationship(
                new Uri("media/" + placeholder.Key + _imageCounter, UriKind.Relative),
                TargetMode.Internal, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image");

            //AddImageToBody(wordprocessingDocument, imageRelationshipPart.Id);



            //ImagePart imagePart = mainPart.AddImagePart(ImageHandler.GetImagePartTypeFromMemStream(imageMemoryStream));

            //imagePart.FeedData(placeholder.Value);

            var imgTmp = ImageHandler.GetImageFromStream(placeholder.Value);

            var drawing = GetImageElement(imageRelationshipPart.Id, placeholder.Key, "picture", imgTmp.Width, imgTmp.Height);

            //var drawing = GetImageElement(wordprocessingDocument, mainPart.GetIdOfPart(imagePart));
            element.Parent.InsertAfter(new Paragraph(new Run(drawing)), element);

        }



        private Drawing GetImageElement(
            string imagePartId,
            string fileName,
            string pictureName,
            double width,
            double height)
        {
            double englishMetricUnitsPerInch = 914400;
            double pixelsPerInch = 96;

            //calculate size in emu
            double emuWidth = width * englishMetricUnitsPerInch / pixelsPerInch;
            double emuHeight = height * englishMetricUnitsPerInch / pixelsPerInch;

            var element = new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = (Int64Value)emuWidth, Cy = (Int64Value)emuHeight },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = (UInt32Value)1U, Name = pictureName + _imageCounter },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = (UInt32Value)0U, Name = fileName  },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip(
                                        new A.BlipExtensionList(
                                            new A.BlipExtension { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" }))
                                    {
                                        Embed = imagePartId,
                                        CompressionState = A.BlipCompressionValues.Print
                                    },
                                            new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = (Int64Value)emuWidth, Cy = (Int64Value)emuHeight }),
                                    new A.PresetGeometry(
                                        new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle })))
                        {
                            Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
                        }))
                {
                    DistanceFromTop = (UInt32Value)0U,
                    DistanceFromBottom = (UInt32Value)0U,
                    DistanceFromLeft = (UInt32Value)0U,
                    DistanceFromRight = (UInt32Value)0U,
                    EditId = "50D07946"
                });
            return element;
        }

        private static void CleanMarkup(WordprocessingDocument doc)
        {
            //REMOVE THESE Markups, because they break up the text into multiple pieces, 
            //thereby preventing simple search and replace
            SimplifyMarkupSettings settings = new SimplifyMarkupSettings
            {
                RemoveComments = true,
                RemoveContentControls = true,
                RemoveEndAndFootNotes = true,
                RemoveFieldCodes = false,
                RemoveLastRenderedPageBreak = true,
                RemovePermissions = true,
                RemoveProof = true,
                RemoveRsidInfo = true,
                RemoveSmartTags = true,
                RemoveSoftHyphens = true,
                ReplaceTabsWithSpaces = true
            };
            MarkupSimplifier.SimplifyMarkup(doc, settings);
        }


        public static void InsertAPicture(string document, string fileName)
        {
            using (WordprocessingDocument wordprocessingDocument =
                WordprocessingDocument.Open(document, true))
            {
                MainDocumentPart mainPart = wordprocessingDocument.MainDocumentPart;

                Uri imageUri = new Uri("/word/media/" +
                  System.IO.Path.GetFileName(fileName), UriKind.Relative);

                // Create "image" part in /word/media
                // Change content type for other image types.
                PackagePart packageImagePart =
                  wordprocessingDocument.Package.CreatePart(imageUri, "Image/jpeg");

                // Feed data.
                byte[] imageBytes = File.ReadAllBytes(fileName);
                packageImagePart.GetStream().Write(imageBytes, 0, imageBytes.Length);

                PackagePart documentPackagePart =
                  mainPart.OpenXmlPackage.Package.GetPart(new Uri("/word/document.xml", UriKind.Relative));

                Console.Out.WriteLine(documentPackagePart.Uri);

                // URI to the image is relative to relationship document.
                PackageRelationship imageRelationshipPart = documentPackagePart.CreateRelationship(
                      new Uri("media/" + System.IO.Path.GetFileName(fileName), UriKind.Relative),
                      TargetMode.Internal, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image");

                AddImageToBody(wordprocessingDocument, imageRelationshipPart.Id);
            }
        }

        private static void AddImageToBody(WordprocessingDocument wordDoc, string relationshipId)
        {
            var element =
                 new Drawing(
                     new DW.Inline(
                         new DW.Extent() { Cx = 990000L, Cy = 792000L },
                         new DW.EffectExtent()
                         {
                             LeftEdge = 0L,
                             TopEdge = 0L,
                             RightEdge = 0L,
                             BottomEdge = 0L
                         },
                         new DW.DocProperties()
                         {
                             Id = (UInt32Value)1U,
                             Name = "Picture 1"
                         },
                         new DW.NonVisualGraphicFrameDrawingProperties(
                             new A.GraphicFrameLocks() { NoChangeAspect = true }),
                         new A.Graphic(
                             new A.GraphicData(
                                 new PIC.Picture(
                                     new PIC.NonVisualPictureProperties(
                                         new PIC.NonVisualDrawingProperties()
                                         {
                                             Id = (UInt32Value)0U,
                                             Name = "New Bitmap Image.jpg"
                                         },
                                         new PIC.NonVisualPictureDrawingProperties()),
                                     new PIC.BlipFill(
                                         new A.Blip(
                                             new A.BlipExtensionList(
                                                 new A.BlipExtension()
                                                 {
                                                     Uri =
                                                     "{28A0092B-C50C-407E-A947-70E740481C1C}"
                                                 })
                                         )
                                         {
                                             Embed = relationshipId,
                                             CompressionState =
                                           A.BlipCompressionValues.Print
                                         },
                                         new A.Stretch(
                                             new A.FillRectangle())),
                                     new PIC.ShapeProperties(
                                         new A.Transform2D(
                                             new A.Offset() { X = 0L, Y = 0L },
                                             new A.Extents() { Cx = 990000L, Cy = 792000L }),
                                         new A.PresetGeometry(
                                             new A.AdjustValueList()
                                         )
                                         { Preset = A.ShapeTypeValues.Rectangle }))
                             )
                             { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                     )
                     {
                         DistanceFromTop = (UInt32Value)0U,
                         DistanceFromBottom = (UInt32Value)0U,
                         DistanceFromLeft = (UInt32Value)0U,
                         DistanceFromRight = (UInt32Value)0U,
                         EditId = "50D07946"
                     });

            wordDoc.MainDocumentPart.Document.Body.AppendChild(
              new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                new DocumentFormat.OpenXml.Wordprocessing.Run(element)));
        }


    }
}
