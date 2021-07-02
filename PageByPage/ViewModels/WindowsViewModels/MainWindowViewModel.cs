using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ITextPdf = iText.Kernel.Pdf;
using ITextLayout = iText.Layout;
using MgMvvmTools;
using Microsoft.Win32;
using PageByPage.Extensions;
using PageByPage.Models;
using Patagames.Pdf.Net.Controls.Wpf;
using PdfiumPdf = Patagames.Pdf;
using PdfiumPdfNet = Patagames.Pdf.Net;
using PdfiumPdfWinForms = Patagames.Pdf.Net.Controls.WinForms;
using PdfDocument = Patagames.Pdf.Net.PdfDocument;
using Point = System.Drawing.Point;

namespace PageByPage.ViewModels.WindowsViewModels
{
    public class MainWindowViewModel : BaseViewModel, IDataErrorInfo
    {
        private string _filePath;
        private string _filePickerHelperText;

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFileSelected));
            }
        }

        public string FilePickerHelperText
        {
            get => _filePickerHelperText;
            set
            {
                _filePickerHelperText = value;
                OnPropertyChanged();
            }
        }

        public string PrinterName { get; set; }

        public IReadOnlyCollection<string> Printers { get; } =
            new ReadOnlyCollection<string>(PrinterSettings.InstalledPrinters.Cast<string>().ToList());

        public MainWindowViewModel()
        {
            PrinterName = Printers.FirstOrDefault();
        }

        public FileInfo CurrentFileInfo { get; set; }
        public PdfiumPdfNet::PdfDocument CurrentPdfDocument { get; set; }

        public bool IsFileSelected => !string.IsNullOrWhiteSpace(FilePath) && this[nameof(FilePath)] == "";

        public ICommand PickFileCommand => new Command(() =>
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PDF, DOCX, DOC|*.pdf;*.docx;*.doc"
            };
            if (openFileDialog.ShowDialog() != true) return;
            FilePath = openFileDialog.FileName;
        });

        public string Error { get; }

        public string this[string columnName] => columnName switch
        {
            nameof(FilePath) => Validation.Validate(() => string.IsNullOrWhiteSpace(FilePath),
                new[]
                {
                    new ValidationCondition(() => File.Exists(FilePath), "File does not exist"),
                    new ValidationCondition(() =>
                    {
                        var filePath = FilePath.ToLower(CultureInfo.InvariantCulture);
                        //return new[] { ".pdf", ".docx", ".doc" }
                        return new[] { ".pdf" }
                            .Any(format => filePath.EndsWith(format, StringComparison.InvariantCulture));
                    }, "Only PDF format is supported")
                }, isValid =>
                {
                    if (string.IsNullOrWhiteSpace(FilePath) || !isValid)
                    {
                        FilePickerHelperText = "";
                        return;
                    }

                    OnFileOpened();
                }),
            _ => ""
        };

        private void OnFileOpened()
        {
            CurrentFileInfo = new FileInfo(FilePath);

            CurrentPdfDocument = PdfiumPdfNet::PdfDocument.Load(FilePath);
            FilePickerHelperText = $"{CurrentFileInfo.Name} - {CurrentPdfDocument.Pages.Count}";
        }

        private void PrintPage(int pageIndex)
        {
            var page = CurrentPdfDocument.Pages[pageIndex];
            var pagePrintDocument = new PdfPrintDocument(page.Document);
            pagePrintDocument.PrinterSettings.PrinterName = PrinterName;
            pagePrintDocument.Print();
        }
    }
}
