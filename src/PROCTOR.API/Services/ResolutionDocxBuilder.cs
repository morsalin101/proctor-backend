using System.IO.Compression;
using System.Security;
using PROCTOR.Domain.Entities;

namespace PROCTOR.API.Services;

public static class ResolutionDocxBuilder
{
    public static byte[] Build(DisciplinaryResolution resolution)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Write(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
                </Types>
                """);
            Write(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
                </Relationships>
                """);

            var body = new System.Text.StringBuilder();
            Paragraph(body, "Disciplinary Committee Resolution", true);
            Paragraph(body, $"Resolution: {resolution.ResolutionNumber}", true);
            Paragraph(body, $"Created by: {resolution.CreatedByName}");
            ResolutionTable(body, resolution.Cases.OrderBy(x => x.DisplayOrder));
            if (resolution.ApprovedAt.HasValue)
                Paragraph(body, $"Approved by {resolution.ApprovedByName} on {resolution.ApprovedAt:yyyy-MM-dd HH:mm} UTC", true);

            var document = $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:body>{body}<w:sectPr><w:pgSz w:w="16838" w:h="11906" w:orient="landscape"/><w:pgMar w:top="1080" w:right="1080" w:bottom="1080" w:left="1080"/></w:sectPr></w:body>
                </w:document>
                """;
            Write(archive, "word/document.xml", document);
        }
        return output.ToArray();
    }

    private static void Paragraph(System.Text.StringBuilder sb, string? value, bool bold = false)
    {
        var text = SecurityElement.Escape(value ?? string.Empty);
        sb.Append("<w:p><w:r>");
        if (bold) sb.Append("<w:rPr><w:b/></w:rPr>");
        sb.Append("<w:t xml:space=\"preserve\">").Append(text).Append("</w:t></w:r></w:p>");
    }

    private static void ResolutionTable(System.Text.StringBuilder sb, IEnumerable<DisciplinaryResolutionCase> cases)
    {
        sb.Append("<w:tbl><w:tblPr><w:tblW w:w=\"10000\" w:type=\"dxa\"/><w:tblLayout w:type=\"fixed\"/>")
            .Append("<w:tblBorders><w:top w:val=\"single\" w:sz=\"8\" w:color=\"000000\"/>")
            .Append("<w:left w:val=\"single\" w:sz=\"8\" w:color=\"000000\"/><w:bottom w:val=\"single\" w:sz=\"8\" w:color=\"000000\"/>")
            .Append("<w:right w:val=\"single\" w:sz=\"8\" w:color=\"000000\"/><w:insideH w:val=\"single\" w:sz=\"6\" w:color=\"808080\"/>")
            .Append("<w:insideV w:val=\"single\" w:sz=\"6\" w:color=\"808080\"/></w:tblBorders></w:tblPr>");

        TableRow(sb,
            ("Serial", 800),
            ("Case ID", 1800),
            ("Accused Information", 4000),
            ("Punishment", 3400),
            header: true);

        foreach (var item in cases)
        {
            TableRow(sb,
                (item.DisplayOrder.ToString(), 800),
                (item.Case.CaseNumber, 1800),
                (AccusedInformation(item.Case), 4000),
                (item.SecretaryRemarks, 3400));
        }
        sb.Append("</w:tbl>");
    }

    private static void TableRow(System.Text.StringBuilder sb, params (string Value, int Width)[] cells) =>
        TableRow(sb, cells, false);

    private static void TableRow(System.Text.StringBuilder sb, (string Value, int Width) cell1, (string Value, int Width) cell2,
        (string Value, int Width) cell3, (string Value, int Width) cell4, bool header) =>
        TableRow(sb, [cell1, cell2, cell3, cell4], header);

    private static void TableRow(System.Text.StringBuilder sb, IEnumerable<(string Value, int Width)> cells, bool header)
    {
        sb.Append("<w:tr>");
        foreach (var (value, width) in cells)
        {
            sb.Append("<w:tc><w:tcPr><w:tcW w:w=\"").Append(width).Append("\" w:type=\"dxa\"/>");
            if (header) sb.Append("<w:shd w:val=\"clear\" w:fill=\"D9EAF7\"/>");
            sb.Append("</w:tcPr><w:p><w:r>");
            if (header) sb.Append("<w:rPr><w:b/></w:rPr>");
            sb.Append("<w:t xml:space=\"preserve\">").Append(SecurityElement.Escape(value ?? string.Empty)).Append("</w:t></w:r></w:p></w:tc>");
        }
        sb.Append("</w:tr>");
    }

    private static string AccusedInformation(Case c)
    {
        var accused = c.AccusedPersons.OrderBy(x => x.Order).Select(x =>
        {
            var details = new List<string> { x.Name };
            if (!string.IsNullOrWhiteSpace(x.AccusedStudentId)) details.Add($"ID: {x.AccusedStudentId}");
            if (!string.IsNullOrWhiteSpace(x.Department)) details.Add($"Department: {x.Department}");
            if (!string.IsNullOrWhiteSpace(x.Contact)) details.Add($"Contact: {x.Contact}");
            if (!string.IsNullOrWhiteSpace(x.GuardianContact)) details.Add($"Guardian: {x.GuardianContact}");
            return string.Join(" | ", details);
        }).ToList();
        if (accused.Count > 0) return string.Join("; ", accused);
        if (string.IsNullOrWhiteSpace(c.AccusedName)) return "Not provided";

        var legacy = new List<string> { c.AccusedName };
        if (!string.IsNullOrWhiteSpace(c.AccusedId)) legacy.Add($"ID: {c.AccusedId}");
        if (!string.IsNullOrWhiteSpace(c.AccusedDepartment)) legacy.Add($"Department: {c.AccusedDepartment}");
        if (!string.IsNullOrWhiteSpace(c.AccusedContact)) legacy.Add($"Contact: {c.AccusedContact}");
        if (!string.IsNullOrWhiteSpace(c.AccusedGuardianContact)) legacy.Add($"Guardian: {c.AccusedGuardianContact}");
        return string.Join(" | ", legacy);
    }

    private static void Write(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(false));
        writer.Write(content.TrimStart());
    }
}
