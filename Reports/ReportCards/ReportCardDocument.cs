using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AutoTable.Reports.ReportCards.Components;

namespace AutoTable.Reports.ReportCards
{
    /// <summary>
    /// Main orchestrator that composes a single-page A4 report card PDF using QuestPDF.
    /// Follows the component tree architecture from the design specification.
    /// 
    /// Component tree:
    ///   ReportCardDocument
    ///   └── Page
    ///       └── ReportCardFrame (outer navy border)
    ///           ├── ReportCardHeader (logo | identity | ribbon)
    ///           ├── StudentInformation (field grid | photo)
    ///           ├── ResultsTable (title bar + subject table)
    ///           ├── SummaryStrip (5 summary boxes)
    ///           ├── [Row: TeacherComments | GradingKey]
    ///           ├── PrincipalRemark
    ///           ├── SignatureSection
    ///           └── ReportCardFooter
    /// </summary>
    public sealed class ReportCardDocument : IDocument
    {
        private readonly ReportCardData _data;
        private readonly ReportCardTheme _theme;

        public ReportCardDocument(ReportCardData data)
        {
            _data = data;
            _theme = ReportCardTheme.From(
                _data.School.PrimaryColor,
                _data.School.SecondaryColor,
                _data.School.AccentColor,
                _data.School.TableBgColor,
                _data.School.GridColor,
                _data.School.BodyFont,
                _data.School.DisplayFont);
        }

        public DocumentMetadata GetMetadata() => new()
        {
            Title = $"Report Card - {_data.Student.Name}",
            Author = _data.School.Name,
            Subject = "Academic Report Card"
        };

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);

                // Default text style for the entire page
                page.DefaultTextStyle(text => text
                    .FontFamily(_theme.BodyFont)
                    .FontSize(8.5f)
                    .FontColor(_theme.Text));

                // Content with minimal padding (design uses nearly full A4)
                page.Content()
                    .Padding(2, Unit.Millimetre)
                    .Element(ComposeReportCard);
            });
        }

        private void ComposeReportCard(IContainer container)
        {
            container
                .Border(_theme.OuterBorderThickness)
                .BorderColor(_theme.PrimaryNavy)
                .Column(column =>
                {
                    column.Spacing(3);

                    // Header: Logo | School Identity | Academic Year Ribbon
                    column.Item().Element(c =>
                        new ReportCardHeader(_data.School, _theme).Compose(c));

                    // Student Information: Field Grid | Photo
                    column.Item().PaddingTop(2).Element(c =>
                        new StudentInformation(_data.Student, _theme).Compose(c));

                    // Examination Results Table
                    column.Item().PaddingTop(2).Element(c =>
                        new ResultsTable(_data.Subjects, _theme).Compose(c));

                    // Summary Strip
                    column.Item().PaddingTop(2).Element(c =>
                        new SummaryStrip(_data.Student, _theme).Compose(c));

                    // Teacher Comments + Grading Key (side by side)
                    column.Item().PaddingTop(3).Row(row =>
                    {
                        row.Spacing(4);
                        row.RelativeItem(0.49f).Element(c =>
                            new TeacherComments(_data.Student, _theme).Compose(c));
                        row.RelativeItem(0.51f).Element(c =>
                            new GradingKey(_data.GradingScale, _theme).Compose(c));
                    });

                    // Principal's Remark
                    column.Item().PaddingTop(3).Element(c =>
                        new PrincipalRemark(_data.Student, _data.School, _theme).Compose(c));

                    // Signatures
                    column.Item().PaddingTop(4).Element(c =>
                        new SignatureSection(_data.Student, _data.School, _theme).Compose(c));

                    // Footer
                    column.Item().PaddingTop(3).Element(c =>
                        new ReportCardFooter(_data.School, _theme).Compose(c));
                });
        }

        // --- Static convenience methods ---

        /// <summary>
        /// Generates a PDF byte array from the provided data.
        /// </summary>
        public static byte[] GeneratePdf(ReportCardData data)
        {
            var doc = new ReportCardDocument(data);
            return doc.GeneratePdf();
        }

        /// <summary>
        /// Generates multiple PDF pages (one per student).
        /// </summary>
        public static byte[] GenerateMultiPagePdf(IReadOnlyList<ReportCardData> reports)
        {
            if (reports == null || reports.Count == 0)
                throw new ArgumentException("At least one report is required.", nameof(reports));

            var doc = Document.Create(container =>
            {
                foreach (var report in reports)
                {
                    var pageDoc = new ReportCardDocument(report);
                    // Compose each page
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(text => text
                            .FontFamily(report.School.BodyFont)
                            .FontSize(8.5f)
                            .FontColor("#17213A"));
                        page.Content()
                            .Padding(2, Unit.Millimetre)
                            .Element(c => pageDoc.ComposeReportCard(c));
                    });
                }
            });

            return doc.GeneratePdf();
        }
    }
}