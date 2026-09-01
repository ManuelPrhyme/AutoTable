using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using AutoTable.Reports.ReportCards.Styling;

namespace AutoTable.Reports.ReportCards.Components
{
    /// <summary>
    /// Grading key table: Grade | Range | Remark
    /// </summary>
    public sealed class GradingKey : IComponent
    {
        private readonly IReadOnlyList<GradeBand> _gradingScale;
        private readonly ReportCardTheme _theme;

        public GradingKey(IReadOnlyList<GradeBand> gradingScale, ReportCardTheme theme)
        {
            _gradingScale = gradingScale;
            _theme = theme;
        }

        public void Compose(IContainer container)
        {
            container.Column(col =>
            {
                // Panel header
                col.Item().Element(c => ReportCardStyles.PanelHeader(c, _theme, "GRADING KEY"));

                // Table
                col.Item()
                    .Border(0.6f)
                    .BorderColor(_theme.GridBlue)
                    .BorderTop(0)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(0.20f);  // Grade
                            columns.RelativeColumn(0.35f);  // Range
                            columns.RelativeColumn(0.45f);  // Remark
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(c => ReportCardStyles.TableHeaderCell(c, _theme)).Text("GRADE");
                            header.Cell().Element(c => ReportCardStyles.TableHeaderCell(c, _theme)).Text("RANGE");
                            header.Cell().Element(c => ReportCardStyles.TableHeaderCell(c, _theme)).Text("REMARK");
                        });

                        // Body
                        foreach (var band in _gradingScale)
                        {
                            table.Cell().Element(c => ReportCardStyles.TableBodyCell(c, _theme))
                                .AlignCenter()
                                .Text(band.Grade)
                                .Bold();

                            table.Cell().Element(c => ReportCardStyles.TableBodyCell(c, _theme))
                                .Text(band.RangeDisplay);

                            table.Cell().Element(c => ReportCardStyles.TableBodyCell(c, _theme))
                                .Text(band.Remark);
                        }
                    });
            });
        }
    }
}