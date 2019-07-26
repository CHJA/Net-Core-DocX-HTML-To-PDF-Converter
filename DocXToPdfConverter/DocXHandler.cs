﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocXToPdfConverter;
using OpenXmlPowerTools;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using TableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;


namespace Website.BackgroundWorkers
{
    public class DocXHandler

    {
        private MemoryStream _docxMs;
        private ReplacementDictionaries _rep;
        private int _imageCounter;

        public DocXHandler(string docXTemplateFilename, ReplacementDictionaries rep)
        {
            _docxMs = StreamHandler.GetFileAsMemoryStream(docXTemplateFilename);
            _rep = rep;
            
        }

        public MemoryStream ReplaceAll()
        {
            if (_rep.TextReplacements.Count > 0)
            {
                ReplaceTexts();
            }

            if (_rep.TableReplacements.Count > 0 && _rep.TableReplacements.First().Count > 0)
            {
                ReplaceTableRows();
            }
            if (_rep.ImageReplacements.Count > 0)
            { 
                ReplaceImages();
            }
            _docxMs.Position = 0;

            return _docxMs;
        }
       

        public MemoryStream ReplaceTexts()
        {
            if (_rep.TextReplacements.Count == 0 || _rep.TextReplacements == null)
                return null;
            using (WordprocessingDocument doc =
                WordprocessingDocument.Open(_docxMs, true))
            {
                CleanMarkup(doc);

                var document = doc.MainDocumentPart.Document;

                foreach (var text in document.Descendants<Text>()) // <<< Here
                {
                    foreach (var replace in _rep.TextReplacements)
                    {
                        if (text.Text.Contains(_rep.TextReplacementStartTag + replace.Key + _rep.TextReplacementEndTag))
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
                                        text.Text = text.Text.Replace(_rep.TextReplacementStartTag + replace.Key + _rep.TextReplacementEndTag, repArray[i]);

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
                                text.Text = text.Text.Replace(_rep.TextReplacementStartTag + replace.Key + _rep.TextReplacementEndTag, replace.Value);

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
            if (_rep.TableReplacements.Count == 0 || _rep.TableReplacements == null)
                return null;

            using (WordprocessingDocument doc =
                WordprocessingDocument.Open(_docxMs, true))
            {

                CleanMarkup(doc);

                var document = doc.MainDocumentPart.Document;

                foreach (var trDict in _rep.TableReplacements) //Take a Row (one Dictionary) at a time
                {
                    var trCol0 = trDict.First();
                    // Find the first text element matching the search string 
                    // where the text is inside a table cell --> this is the row we are searching for.
                    var textElement = document.Body.Descendants<Text>()
                        .FirstOrDefault(t =>
                            t.Text == _rep.TableReplacementStartTag + trCol0.Key + _rep.TableReplacementEndTag &&
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

                                    if (text.Text.Contains(_rep.TableReplacementStartTag + item.Key +
                                                           _rep.TableReplacementEndTag))
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
                                                        _rep.TableReplacementStartTag + item.Key +
                                                        _rep.TableReplacementEndTag, repArray[i]);

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
                                                _rep.TableReplacementStartTag + item.Key + _rep.TableReplacementEndTag,
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





        public MemoryStream ReplaceImages()
        {
            if (_rep.ImageReplacements.Count == 0 || _rep.ImageReplacements == null)
                return null;

            using (WordprocessingDocument doc =
                WordprocessingDocument.Open(_docxMs, true))
            {
                CleanMarkup(doc);

                var document = doc.MainDocumentPart.Document;

                foreach (var text in document.Descendants<Text>()) // <<< Here
                {
                    foreach (var replace in _rep.ImageReplacements)
                    {
                        _imageCounter++;
                        if (text.Text.Contains(_rep.ImageReplacementStartTag + replace.Key + _rep.ImageReplacementEndTag))
                        {
                            
                            text.Text = text.Text.Replace(
                                _rep.ImageReplacementStartTag + replace.Key + _rep.ImageReplacementEndTag,
                                "");
                            
                            AppendImageToElement(replace.Value, text, doc);

                            
                        }

                    }
                }
            }
            _docxMs.Position = 0;
            return _docxMs;

        }


        private void AppendImageToElement(MemoryStream imageMemoryStream, OpenXmlElement element, WordprocessingDocument wordprocessingDocument)
        {

            MainDocumentPart mainPart = wordprocessingDocument.MainDocumentPart;

            ImagePart imagePart = mainPart.AddImagePart(ImageHandler.GetImagePartTypeFromMemStream(imageMemoryStream));

            imagePart.FeedData(imageMemoryStream);

            var imgTmp = ImageHandler.GetImageFromStream(imageMemoryStream);

            var drawing = GetImageElement(mainPart.GetIdOfPart(imagePart), "image", "picture", imgTmp.Width, imgTmp.Height );

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
                                    new PIC.NonVisualDrawingProperties { Id = (UInt32Value)0U, Name = fileName + _imageCounter },
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

        
    }
}
