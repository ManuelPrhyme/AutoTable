using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;

namespace AutoTable.Services
{
    /// <summary>
    /// Detects the printers configured on this machine and prints rendered page bitmaps
    /// directly to a user-chosen printer (bypassing the OS print dialog).
    ///
    /// Printer enumeration uses GDI (System.Drawing.Printing.PrinterSettings.InstalledPrinters)
    /// — a managed, battle-tested path that lists every local and network-configured printer,
    /// including the system default. WinUI 3 desktop apps have no WinRT API to list printers.
    /// </summary>
    public static class PrinterService
    {
        /// <summary>Names of every local / network-connection printer configured on this machine.</summary>
        public static IReadOnlyList<string> GetInstalledPrinters()
        {
            var names = new List<string>();
            try
            {
                foreach (string name in PrinterSettings.InstalledPrinters)
                {
                    if (!string.IsNullOrWhiteSpace(name))
                        names.Add(name);
                }
            }
            catch
            {
                // Printing subsystem unavailable — returns an empty list; UI falls back
                // to the system print dialog.
            }
            return names;
        }

        /// <summary>The system default printer name, or null when there is none.</summary>
        public static string? GetDefaultPrinterName()
        {
            try
            {
                var settings = new PrinterSettings();
                string name = settings.PrinterName;
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Prints one bitmap per page to the specified printer through GDI (no dialog shown).
        /// Pass <paramref name="printerName"/> = "" to use the system default printer.
        /// </summary>
        public static bool PrintBitmaps(string printerName, short copies, IReadOnlyList<Bitmap> pages, out string error)
        {
            error = string.Empty;
            if (pages == null || pages.Count == 0)
            {
                error = "There are no pages to print.";
                return false;
            }

            try
            {
                using var doc = new PrintDocument();

                // Select the user's printer; fall back to the default when unavailable.
                if (!string.IsNullOrWhiteSpace(printerName))
                {
                    try { doc.PrinterSettings.PrinterName = printerName; }
                    catch
                    {
                        // Invalid / offline printer name: keep the default printer.
                    }
                }

                if (!doc.PrinterSettings.IsValid)
                {
                    error = "No printers are installed on this computer, or the printer is offline. " +
                            "Add a printer or choose the system print dialog instead.";
                    return false;
                }

                doc.PrinterSettings.Copies = Math.Max((short)1, copies);

                // Prefer A4, fall back to the printer's default paper.
                foreach (PaperSize ps in doc.PrinterSettings.PaperSizes)
                {
                    if (ps.PaperName.IndexOf("A4", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        doc.DefaultPageSettings.PaperSize = ps;
                        break;
                    }
                }

                int pageIndex = 0;
                doc.PrintPage += (_, e) =>
                {
                    var page = pages[Math.Min(pageIndex, pages.Count - 1)]!;
                    var target = e.MarginBounds.Width > 0 && e.MarginBounds.Height > 0
                        ? e.MarginBounds
                        : e.PageBounds;

                    if (e.Graphics != null)
                    {
                        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                        e.Graphics.DrawImage(page, target);
                    }

                    pageIndex++;
                    e.HasMorePages = pageIndex < pages.Count;
                };

                doc.Print();

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Builds a System.Drawing.Bitmap from raw BGRA8 (premultiplied) pixels, the exact
        /// format WinUI's RenderTargetBitmap.GetPixelsAsync() returns.
        /// </summary>
        public static Bitmap BitmapFromBgraPixels(byte[] pixelData, int width, int height)
        {
            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            var rect = new Rectangle(0, 0, width, height);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            try
            {
                Marshal.Copy(pixelData, 0, data.Scan0, pixelData.Length);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return bmp;
        }
    }
}