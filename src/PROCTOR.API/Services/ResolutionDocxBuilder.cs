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
            foreach (var item in resolution.Cases.OrderBy(x => x.DisplayOrder))
            {
                Paragraph(body, $"{item.DisplayOrder}. Case {item.Case.CaseNumber}", true);
                Paragraph(body, $"Short description: {item.ShortDescription}");
                var remarks = item.Case.Type3Workflow?.MemberRemarks
                    .Where(x => x.SubmittedAt.HasValue).OrderBy(x => x.MemberName).ToList() ?? [];
                Paragraph(body, "DC Member Remarks", true);
                foreach (var remark in remarks)
                    Paragraph(body, $"{remark.MemberName}: {remark.Content}");
                Paragraph(body, $"Secretary Resolution: {item.SecretaryRemarks}", true);
            }
            if (resolution.ApprovedAt.HasValue)
                Paragraph(body, $"Approved by {resolution.ApprovedByName} on {resolution.ApprovedAt:yyyy-MM-dd HH:mm} UTC", true);

            var document = $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:body>{body}<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440"/></w:sectPr></w:body>
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

    private static void Write(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(false));
        writer.Write(content.TrimStart());
    }
}
