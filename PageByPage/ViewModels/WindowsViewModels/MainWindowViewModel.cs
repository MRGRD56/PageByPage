using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BitmapTools.Wpf;
using ITextPdf = iText.Kernel.Pdf;
using ITextLayout = iText.Layout;
using MgMvvmTools;
using Microsoft.Win32;
using PageByPage.Extensions;
using PageByPage.Models;
using Patagames.Pdf.Enums;
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
        private static readonly ReadOnlyCollection<string> SupportedFormats = new(new List<string>
        {
            "PDF", "DOCX", "DOC"
        });

        private string _filePath;
        private string _filePickerHelperText;
        private int _currentPage;
        private PdfDocument _currentPdfDocument;
        private BitmapFrame _currentPageImageSource = BitmapFrame.Create(new Bitmap(3508, 2480).ToBitmapSource());
        private string Settings;

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFileSelected));
                OnFilePathChanged();
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

        public string PrinterName
        {
            get => Properties.Settings.Default.PrinterName;
            set
            {
                Properties.Settings.Default.PrinterName = value;
                Properties.Settings.Default.Save();
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
                if (this.IsValid(nameof(CurrentPage)))
                {
                    OnPageChanged();
                }
            }
        }

        public ReadOnlyCollection<string> Printers { get; } = new(PrinterSettings.InstalledPrinters.Cast<string>().ToList());

        public MainWindowViewModel()
        {
            if (string.IsNullOrWhiteSpace(PrinterName))
            {
                PrinterName = Printers.FirstOrDefault();
            }
        }

        public FileInfo CurrentFileInfo { get; set; }

        public PdfiumPdfNet::PdfDocument CurrentPdfDocument
        {
            get => _currentPdfDocument;
            set
            {
                _currentPdfDocument = value;
                OnPropertyChanged(nameof(IsFileSelected));
            }
        }

        public int? CurrentFilePagesCount => CurrentPdfDocument?.Pages.Count;

        public bool IsFileSelected => !string.IsNullOrWhiteSpace(FilePath) && CurrentPdfDocument != null;

        public BitmapFrame CurrentPageImageSource
        {
            get => _currentPageImageSource;
            set
            {
                _currentPageImageSource = value;
                OnPropertyChanged();
            }
        }

        public ICommand PickFileCommand => new Command(() =>
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = $"{string.Join(", ", SupportedFormats)}|" +
                         $"{string.Join(";", SupportedFormats.Select(format => $"*.{format.ToLowerInvariant()}"))}|" +
                         $"{string.Join("|", SupportedFormats.Select(format => $"{format}|*.{format.ToLowerInvariant()}"))}|" +
                         "All files|*.*"
            };
            if (openFileDialog.ShowDialog() != true) return;
            FilePath = openFileDialog.FileName;
        });

        public ICommand PrintCurrentPageCommand => new Command(() =>
        {
            PrintPage(CurrentPage);
        }, () => IsFileSelected && this.IsValid(nameof(CurrentPage)));

        public ICommand PrintNextPageCommand => new Command(() =>
        {
            PrintPage(++CurrentPage);
        }, () => IsFileSelected && CanGoNextPage && this.IsValid(nameof(CurrentPage)));

        public ICommand PreviousPageCommand => new Command(() =>
        {
            CurrentPage--;
        }, () => CanGoPreviousPage);

        public ICommand NextPageCommand => new Command(() =>
        {
            CurrentPage++;
        }, () => CanGoNextPage);

        private bool CanGoPreviousPage => CurrentPage > 1;
        private bool CanGoNextPage => CurrentPage < CurrentFilePagesCount;

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
                        return SupportedFormats.Select(format => $".{format.ToLowerInvariant()}")
                            .Any(format => filePath.EndsWith(format, StringComparison.InvariantCulture));
                    }, $"Only {string.Join(", ", SupportedFormats)} format(s) are supported")
                }),
            nameof(CurrentPage) => Validation.Validate(() => CurrentFilePagesCount == null,
                new[]
                {
                    new ValidationCondition(() => CurrentPage >= 1 && CurrentPage <= CurrentFilePagesCount, "Page number is out of range")
                }),
            _ => Validation.NoError
        };

        private void OnFilePathChanged()
        {
            var isValid = this.IsValid(nameof(FilePath));
            if (IsFileSelected && (string.IsNullOrWhiteSpace(FilePath) || !isValid))
            {
                OnFileClosed();
                return;
            }

            OnFileOpened();
        }

        private void OnFileClosed()
        {
            CurrentFileInfo = null;
            CurrentPdfDocument = null;
            FilePickerHelperText = "";
        }

        private void OnFileOpened()
        {
            CurrentFileInfo = new FileInfo(FilePath);

            CurrentPdfDocument = PdfiumPdfNet::PdfDocument.Load(FilePath);
            FilePickerHelperText = $"{CurrentFileInfo.Name} - {CurrentPdfDocument.Pages.Count} page(s)";
            CurrentPage = 1;
        }

        private void PrintPage(int pageIndex)
        {
            var pageDocument = PdfDocument.CreateNew();
            pageDocument.Pages.ImportPages(CurrentPdfDocument, pageIndex.ToString(CultureInfo.InvariantCulture), 0);
            var pagePrintDocument = new PdfPrintDocument(pageDocument);
            pagePrintDocument.PrinterSettings.PrinterName = PrinterName;
            pagePrintDocument.Print();
        }

        private Image GetPageImage(int pageIndex)
        {
            var page = CurrentPdfDocument.Pages[pageIndex - 1];
            var width = (int) (page.Width / 72D * 96);
            var height = (int) (page.Height / 72D * 96);

            var pdfBitmap = new PdfiumPdfNet.PdfBitmap(width, height, true);
            page.Render(pdfBitmap, 0, 0, width, height, PageRotate.Normal, RenderFlags.FPDF_LCD_TEXT);
            var pageImage = pdfBitmap.Image;
            return pageImage;
        }

        private async void OnPageChanged()
        {
            var imageSource = await GetPageImage(CurrentPage).ToBitmapSourceAsync();
            CurrentPageImageSource = BitmapFrame.Create(imageSource);
        }
    }
}
