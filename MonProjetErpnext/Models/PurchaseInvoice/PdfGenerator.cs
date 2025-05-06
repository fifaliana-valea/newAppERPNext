using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Layout.Properties;
using MonProjetErpnext.Models.PurchaseInvoice;
using System.IO;
using iText.Layout.Borders;
using iText.Kernel.Colors;

namespace MonProjetErpnext.Models.PurchaseInvoice
{
    public class PdfGenerator
    {
        public byte[] GeneratePurchaseInvoicePdf(PurchaseInvoice invoice)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Font setup
            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Title
            document.Add(new Paragraph("FACTURE")
                .SetFont(boldFont)
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Invoice Info
            document.Add(new Paragraph()
                .Add(new Text("Facture N°: ").SetFont(boldFont))
                .Add(new Text(invoice.Name).SetFont(regularFont)));

            document.Add(new Paragraph()
                .Add(new Text("Fournisseur: ").SetFont(boldFont))
                .Add(new Text(invoice.SupplierName ?? invoice.Supplier).SetFont(regularFont)));

            document.Add(new Paragraph()
                .Add(new Text("Date: ").SetFont(boldFont))
                .Add(new Text(invoice.PostingDate).SetFont(regularFont)));

            // Table des articles
            Table table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 5, 2, 2, 2, 2 })).UseAllAvailableWidth();

            // En-tête
            table.AddHeaderCell(new Cell().Add(new Paragraph("Code")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Nom")));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Qté")));
            table.AddHeaderCell(new Cell().Add(new Paragraph("PU")));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Total")));
            table.AddHeaderCell(new Cell().Add(new Paragraph("UoM")));

            // Données
            foreach (var item in invoice.Items)
            {
                table.AddCell(item.ItemCode);
                table.AddCell(item.ItemName);
                table.AddCell(item.Quantity.ToString());
                table.AddCell(item.Rate.ToString("F2"));
                table.AddCell(item.Amount.ToString("F2"));
                table.AddCell(item.UnitOfMeasure);
            }

            document.Add(table);

            // Totaux
            document.Add(new Paragraph()
                .Add(new Text("\nTotal: ").SetFont(boldFont))
                .Add(new Text($"{invoice.Total:F2} {invoice.Currency}").SetFont(regularFont)));

            document.Add(new Paragraph()
                .Add(new Text("Grand Total: ").SetFont(boldFont))
                .Add(new Text($"{invoice.GrandTotal:F2} {invoice.Currency}").SetFont(regularFont)));

            document.Add(new Paragraph()
                .Add(new Text("Montant dû: ").SetFont(boldFont))
                .Add(new Text($"{invoice.OutstandingAmount:F2} {invoice.Currency}").SetFont(regularFont)));

            document.Close();
            return ms.ToArray();
        }
    }
}
