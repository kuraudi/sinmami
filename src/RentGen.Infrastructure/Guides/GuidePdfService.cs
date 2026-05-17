using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Guides.DTOs;
using RentGen.Domain.Enums;
using RentGen.Infrastructure.Persistence;
using RentGenDocument = RentGen.Domain.Entities.Document;

namespace RentGen.Infrastructure.Guides;

public sealed class GuidePdfService(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IGuideService guideService) : IGuidePdfService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IGuideService _guideService = guideService;

    static GuidePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<GuidePdfResult> GetGuidePdfAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var document = await _dbContext.Documents
            .AsNoTracking()
            .FirstAsync(x => x.Id == documentId && x.UserId == userId, cancellationToken);

        var guide = await _guideService.GetGuideAsync(documentId, cancellationToken);
        var model = BuildModel(document, guide);

        var content = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(
                    TextStyle.Default
                        .FontFamily("Times New Roman")
                        .FontSize(11)
                        .FontColor(Colors.Grey.Darken4)
                        .LineHeight(1.4f));

                page.Header().Element(x => ComposeHeader(x, model));
                page.Content().Element(x => ComposeContent(x, model));
                page.Footer().Element(x => ComposeFooter(x, model));
            });
        }).GeneratePdf();

        return new GuidePdfResult
        {
            FileName = BuildFileName(model),
            ContentType = "application/pdf",
            Content = content
        };
    }

    private static GuidePdfModel BuildModel(RentGenDocument document, GuideResponse guide)
    {
        var blocks = ParseGuideBlocks(guide.Content);
        var heading = blocks.OfType<GuideHeadingBlock>().FirstOrDefault()?.Text
            ?? "\u041f\u0440\u0430\u043a\u0442\u0438\u0447\u0435\u0441\u043a\u0438\u0439 \u0433\u0430\u0439\u0434 \u043f\u043e \u0434\u0430\u043b\u044c\u043d\u0435\u0439\u0448\u0438\u043c \u0434\u0435\u0439\u0441\u0442\u0432\u0438\u044f\u043c";

        return new GuidePdfModel(
            DocumentTitle: document.Title,
            DocumentId: document.Id,
            GuideTypeLabel: guide.GuideType == GuideType.Personalized
                ? "\u041f\u0435\u0440\u0441\u043e\u043d\u0430\u043b\u0438\u0437\u0438\u0440\u043e\u0432\u0430\u043d\u043d\u044b\u0439 mini-guide"
                : "\u0421\u0442\u0430\u043d\u0434\u0430\u0440\u0442\u043d\u044b\u0439 mini-guide",
            GeneratedAtUtc: document.GeneratedAtUtc ?? DateTimeOffset.UtcNow,
            Heading: heading,
            Blocks: blocks.Where(x => x is not GuideHeadingBlock).ToList());
    }

    private static string BuildFileName(GuidePdfModel model)
    {
        var suffix = model.GuideTypeLabel.Contains("\u041f\u0435\u0440\u0441\u043e\u043d\u0430\u043b", StringComparison.Ordinal)
            ? "personalized"
            : "standard";

        return $"rentgen-guide-{suffix}-{model.DocumentId.ToString("N")[..8]}.pdf";
    }

    private static void ComposeHeader(IContainer container, GuidePdfModel model)
    {
        container.Column(column =>
        {
            column.Spacing(8);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("\u042e\u0420\u0418\u0414\u0418\u0427\u0415\u0421\u041a\u0418 \u041e\u0424\u041e\u0420\u041c\u041b\u0415\u041d\u041d\u042b\u0419 MINI-GUIDE")
                        .FontSize(15)
                        .SemiBold()
                        .FontColor(Colors.Teal.Darken2);

                    left.Item().PaddingTop(4).Text(model.Heading)
                        .FontSize(21)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken4);
                });

                row.ConstantItem(150).AlignRight().Column(right =>
                {
                    right.Item().Text("RentGen")
                        .FontSize(16)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken4);

                    right.Item().PaddingTop(4).Text(model.GuideTypeLabel)
                        .FontSize(10)
                        .FontColor(Colors.Grey.Medium);
                });
            });

            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(IContainer container, GuidePdfModel model)
    {
        container.PaddingTop(16).Column(column =>
        {
            column.Spacing(14);

            column.Item()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(14)
                .Background("#FCFBF7")
                .Column(meta =>
                {
                    meta.Spacing(6);
                    meta.Item().Text("\u041e\u0441\u043d\u043e\u0432\u0430\u043d\u0438\u0435 \u0434\u043e\u043a\u0443\u043c\u0435\u043d\u0442\u0430")
                        .FontSize(12)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken3);

                    meta.Item().Text(text =>
                    {
                        text.Span("\u0413\u0430\u0439\u0434 \u043f\u043e\u0434\u0433\u043e\u0442\u043e\u0432\u043b\u0435\u043d \u0434\u043b\u044f \u0434\u043e\u043a\u0443\u043c\u0435\u043d\u0442\u0430: ").SemiBold();
                        text.Span(model.DocumentTitle);
                    });

                    meta.Item().Text(text =>
                    {
                        text.Span("\u0414\u0430\u0442\u0430 \u0444\u043e\u0440\u043c\u0438\u0440\u043e\u0432\u0430\u043d\u0438\u044f: ").SemiBold();
                        text.Span(model.GeneratedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture));
                    });

                    meta.Item().Text(text =>
                    {
                        text.Span("Document ID: ").SemiBold();
                        text.Span(model.DocumentId.ToString());
                    });
                });

            column.Item().Text("\u0420\u0435\u043a\u043e\u043c\u0435\u043d\u0434\u0443\u0435\u043c\u044b\u0435 \u0434\u0435\u0439\u0441\u0442\u0432\u0438\u044f")
                .FontSize(15)
                .SemiBold()
                .FontColor(Colors.Grey.Darken4);

            foreach (var block in model.Blocks)
            {
                switch (block)
                {
                    case GuideStepBlock step:
                        column.Item()
                            .BorderLeft(3)
                            .BorderColor(Colors.Teal.Darken2)
                            .PaddingLeft(12)
                            .PaddingVertical(2)
                            .Column(stepColumn =>
                            {
                                stepColumn.Spacing(4);
                                stepColumn.Item().Text(text =>
                                {
                                    text.DefaultTextStyle(x => x.FontSize(12));
                                    text.Span($"{step.Number}. ").SemiBold().FontColor(Colors.Teal.Darken2);
                                    text.Span(step.Title).SemiBold();
                                });

                                if (!string.IsNullOrWhiteSpace(step.Body))
                                {
                                    stepColumn.Item().Text(step.Body).Justify();
                                }
                            });
                        break;

                    case GuideParagraphBlock paragraph:
                        column.Item().Text(paragraph.Text).Justify();
                        break;
                }
            }

            column.Item()
                .PaddingTop(10)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(14)
                .Background("#F8F5EE")
                .Column(disclaimer =>
                {
                    disclaimer.Spacing(5);
                    disclaimer.Item().Text("\u041f\u0440\u0430\u0432\u043e\u0432\u043e\u0435 \u043f\u0440\u0438\u043c\u0435\u0447\u0430\u043d\u0438\u0435")
                        .FontSize(12)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken3);

                    disclaimer.Item().Text(
                        "\u0414\u0430\u043d\u043d\u044b\u0439 PDF-\u0433\u0430\u0439\u0434 \u043d\u043e\u0441\u0438\u0442 \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0446\u0438\u043e\u043d\u043d\u044b\u0439 \u0445\u0430\u0440\u0430\u043a\u0442\u0435\u0440 \u0438 \u043f\u043e\u043c\u043e\u0433\u0430\u0435\u0442 \u043e\u0440\u0433\u0430\u043d\u0438\u0437\u043e\u0432\u0430\u0442\u044c \u0434\u0430\u043b\u044c\u043d\u0435\u0439\u0448\u0438\u0435 \u0434\u0435\u0439\u0441\u0442\u0432\u0438\u044f \u043f\u043e\u0441\u043b\u0435 \u043f\u043e\u0434\u0433\u043e\u0442\u043e\u0432\u043a\u0438 \u0434\u043e\u0433\u043e\u0432\u043e\u0440\u0430. " +
                        "\u041f\u0440\u0438 \u0432\u044b\u0441\u043e\u043a\u043e\u043c \u0440\u0438\u0441\u043a\u0435, \u0441\u043f\u043e\u0440\u0435 \u043c\u0435\u0436\u0434\u0443 \u0441\u0442\u043e\u0440\u043e\u043d\u0430\u043c\u0438 \u0438\u043b\u0438 \u043d\u0435\u0442\u0438\u043f\u043e\u0432\u043e\u0439 \u0441\u0438\u0442\u0443\u0430\u0446\u0438\u0438 \u0440\u0435\u043a\u043e\u043c\u0435\u043d\u0434\u0443\u0435\u0442\u0441\u044f \u0434\u043e\u043f\u043e\u043b\u043d\u0438\u0442\u0435\u043b\u044c\u043d\u0430\u044f \u044e\u0440\u0438\u0434\u0438\u0447\u0435\u0441\u043a\u0430\u044f \u043f\u0440\u043e\u0432\u0435\u0440\u043a\u0430."
                    ).Justify();
                });
        });
    }

    private static void ComposeFooter(IContainer container, GuidePdfModel model)
    {
        container.Column(column =>
        {
            column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium));
                    text.Span("RentGen");
                    text.Span(" | ");
                    text.Span(model.GuideTypeLabel);
                });

                row.ConstantItem(90).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium));
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });
    }

    private static IReadOnlyList<GuideBlock> ParseGuideBlocks(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var result = new List<GuideBlock>();
        var lines = content.Replace("\r\n", "\n").Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var currentLine = lines[index].Trim();
            if (string.IsNullOrWhiteSpace(currentLine))
            {
                continue;
            }

            var firstLine = CleanMarkdown(currentLine);
            if (firstLine.StartsWith("### ", StringComparison.Ordinal))
            {
                result.Add(new GuideHeadingBlock(firstLine[4..].Trim()));
                continue;
            }

            var stepMatch = Regex.Match(firstLine, @"^(?<number>\d+)\.\s+(?<title>.+)$");
            if (stepMatch.Success)
            {
                var title = CleanMarkdown(stepMatch.Groups["title"].Value);
                var bodyLines = new List<string>();

                while (index + 1 < lines.Length)
                {
                    var nextLineRaw = lines[index + 1].Trim();
                    if (string.IsNullOrWhiteSpace(nextLineRaw))
                    {
                        index++;
                        if (bodyLines.Count > 0)
                        {
                            break;
                        }

                        continue;
                    }

                    var nextLine = CleanMarkdown(nextLineRaw);
                    if (nextLine.StartsWith("### ", StringComparison.Ordinal) ||
                        Regex.IsMatch(nextLine, @"^\d+\.\s+.+$"))
                    {
                        break;
                    }

                    bodyLines.Add(nextLine);
                    index++;
                }

                var body = string.Join(" ", bodyLines);
                result.Add(new GuideStepBlock(stepMatch.Groups["number"].Value, title, body));
                continue;
            }

            var paragraphLines = new List<string> { firstLine };

            while (index + 1 < lines.Length)
            {
                var nextLineRaw = lines[index + 1].Trim();
                if (string.IsNullOrWhiteSpace(nextLineRaw))
                {
                    index++;
                    break;
                }

                var nextLine = CleanMarkdown(nextLineRaw);
                if (nextLine.StartsWith("### ", StringComparison.Ordinal) ||
                    Regex.IsMatch(nextLine, @"^\d+\.\s+.+$"))
                {
                    break;
                }

                paragraphLines.Add(nextLine);
                index++;
            }

            var paragraph = string.Join(" ", paragraphLines);
            result.Add(new GuideParagraphBlock(paragraph));
        }

        return result;
    }

    private static string CleanMarkdown(string value)
    {
        return value
            .Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("__", string.Empty, StringComparison.Ordinal)
            .Replace("`", string.Empty, StringComparison.Ordinal)
            .Replace("- ", string.Empty, StringComparison.Ordinal)
            .Replace("* ", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private sealed record GuidePdfModel(
        string DocumentTitle,
        Guid DocumentId,
        string GuideTypeLabel,
        DateTimeOffset GeneratedAtUtc,
        string Heading,
        IReadOnlyList<GuideBlock> Blocks);

    private abstract record GuideBlock;
    private sealed record GuideHeadingBlock(string Text) : GuideBlock;
    private sealed record GuideStepBlock(string Number, string Title, string Body) : GuideBlock;
    private sealed record GuideParagraphBlock(string Text) : GuideBlock;
}
