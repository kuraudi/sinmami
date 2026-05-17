using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentGen.Application.Appendices.DTOs;
using RentGen.Application.Common.Interfaces;
using RentGen.Infrastructure.Persistence;

namespace RentGen.Infrastructure.Appendices;

public sealed class AppendixPdfService(
    AppDbContext dbContext,
    ICurrentUserService currentUserService) : IAppendixPdfService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    static AppendixPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<AppendixPdfResult> GetAppendixPdfAsync(Guid appendixId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var appendix = await _dbContext.AppendixDocuments
            .AsNoTracking()
            .FirstAsync(x => x.Id == appendixId && x.UserId == userId, cancellationToken);

        return BuildResult(appendix.Title, appendix.Content, $"rentgen-appendix-{appendix.AppendixType}");
    }

    public Task<AppendixPdfResult> GetPreviewPdfAsync(AppendixPreviewResponse preview, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Task.FromResult(BuildResult(preview.Title, preview.Content, $"rentgen-preview-{preview.AppendixType}"));
    }

    private static AppendixPdfResult BuildResult(string title, string content, string fallbackPrefix)
    {
        var pdf = CreatePdfDocument(new AppendixPdfModel(
            title,
            content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList())).GeneratePdf();

        return new AppendixPdfResult
        {
            FileName = BuildFileName(title, fallbackPrefix),
            ContentType = "application/pdf",
            Content = pdf
        };
    }

    private static IDocument CreatePdfDocument(AppendixPdfModel model)
    {
        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(42);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(
                    TextStyle.Default
                        .FontFamily("Times New Roman")
                        .FontSize(11)
                        .FontColor(Colors.Grey.Darken4)
                        .LineHeight(1.38f));

                page.Header().Element(ComposeHeader);
                page.Content().Element(content => ComposeContent(content, model));
                page.Footer().Element(ComposeFooter);
            });
        });
    }

    private static void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text("RentGen").FontSize(12).SemiBold().FontColor(Colors.Teal.Darken2);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(IContainer container, AppendixPdfModel model)
    {
        container.PaddingTop(14).Column(column =>
        {
            column.Spacing(4);

            foreach (var rawLine in model.Lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    column.Item().PaddingTop(6);
                    continue;
                }

                var line = rawLine.Trim();
                var style = ResolveLineStyle(line);
                var item = column.Item();

                if (style.AddTopPadding)
                {
                    item = item.PaddingTop(6);
                }

                var text = item.Text(line).FontSize(style.FontSize).FontColor(style.Color);

                if (style.IsBold)
                {
                    text = text.SemiBold();
                }

                if (style.AlignCenter)
                {
                    text.AlignCenter();
                }
                else
                {
                    text.Justify();
                }
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem();
            row.ConstantItem(80).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private static LineStyle ResolveLineStyle(string line)
    {
        if (IsUppercaseHeading(line))
        {
            return new LineStyle(true, true, 13, Colors.Grey.Darken4, true);
        }

        if (line.Equals("РЕКВИЗИТЫ И ПОДПИСИ СТОРОН", StringComparison.OrdinalIgnoreCase))
        {
            return new LineStyle(true, true, 12.5f, Colors.Grey.Darken4, true);
        }

        if (Regex.IsMatch(line, @"^\d+(\.\d+)*\.", RegexOptions.CultureInvariant) ||
            line.EndsWith(":", StringComparison.Ordinal))
        {
            return new LineStyle(true, false, 11.5f, Colors.Grey.Darken4, true);
        }

        if (line.StartsWith("г.", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("«", StringComparison.Ordinal) ||
            line.StartsWith("\"", StringComparison.Ordinal))
        {
            return new LineStyle(true, false, 11, Colors.Grey.Darken3, false);
        }

        return new LineStyle(false, false, 11, Colors.Grey.Darken4, false);
    }

    private static bool IsUppercaseHeading(string line)
    {
        return !Regex.IsMatch(line, @"^\d", RegexOptions.CultureInvariant) &&
               line.Equals(line.ToUpperInvariant(), StringComparison.Ordinal) &&
               line.Length <= 100;
    }

    private static string BuildFileName(string title, string fallbackPrefix)
    {
        var normalized = title.ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9а-яё]+", "-", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"-+", "-", RegexOptions.CultureInvariant).Trim('-');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = fallbackPrefix;
        }

        return $"{normalized}.pdf";
    }

    private sealed record AppendixPdfModel(string Title, IReadOnlyList<string> Lines);

    private sealed record LineStyle(bool AddTopPadding, bool AlignCenter, float FontSize, string Color, bool IsBold);
}
