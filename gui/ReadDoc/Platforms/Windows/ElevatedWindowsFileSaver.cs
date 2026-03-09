#if WINDOWS
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Storage;

namespace ReadDoc.Platforms.Windows
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileName
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;

        public string? filter = null;
        public string? customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;

        public string? file = null;
        public int maxFile = 0;

        public string? fileTitle = null;
        public int maxFileTitle = 0;

        public string? initialDir = null;

        public string? title = null;

        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;

        public string? defExt = null;

        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;

        public string? templateName = null;

        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }

    public class LibWrap
    {
        //BOOL GetOpenFileName(LPOPENFILENAME lpofn);

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
        public static extern int CommDlgExtendedError();
    }

    /// <summary>
    /// Windows implementation of IFileSaver that uses the Win32 Save File dialog,
    /// compatible with elevated processes.
    /// </summary>
    public sealed class ElevatedWindowsFileSaver : IFileSaver
    {
        public async Task<FileSaverResult> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken = default)
        {
            // Ensure this runs on the UI thread; Win32 dialog expects anSTA owner.
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    // Suggest extension based on file name
                    var ext = Path.GetExtension(fileName);
                    var filter = BuildFilter(ext);

                    var selectedPath = ShowSaveFileDialog(ownerHwnd: GetActiveWindowHandle(),
                                                          suggestedName: fileName,
                                                          filter: filter,
                                                          defaultExt: string.IsNullOrWhiteSpace(ext) ? null : ext.TrimStart('.'));

                    if (string.IsNullOrWhiteSpace(selectedPath))
                    {
                        // User canceled
                        return new FileSaverResult(null, null);
                    }

                    // Write stream to the chosen path
                    Directory.CreateDirectory(Path.GetDirectoryName(selectedPath)!);

                    using var fs = new FileStream(selectedPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await stream.CopyToAsync(fs, cancellationToken);
                    await fs.FlushAsync(cancellationToken);

                    return new FileSaverResult(selectedPath, null);
                }
                catch (Exception ex)
                {
                    return new FileSaverResult(null, ex);
                }
            });
        }

        // Build a basic filter string ("Description|pattern|...") converted to COMDLG32 format
        private static string BuildFilter(string? extension)
        {
            // Example: if ext=".txt" -> "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            if (!string.IsNullOrWhiteSpace(extension))
            {
                var extNoDot = extension.TrimStart('.');
                return $"*{extension} files (*{extension})|*{extension}|All files (*.*)|*.*";
            }
            return "All files (*.*)|*.*";
        }

        /// <summary>
        /// Shows the Win32 Save File dialog using GetSaveFileName.
        /// </summary>
        private static string? ShowSaveFileDialog(IntPtr ownerHwnd, string suggestedName, string filter, string? defaultExt)
        {
            // Convert "Desc|pattern|Desc|pattern" -> "Desc\0pattern\0Desc\0pattern\0\0"
            string filterForApi = filter.Replace('|', '\0') + "\0";

            OpenFileName ofn = new OpenFileName();

            ofn.structSize = Marshal.SizeOf(ofn);

            ofn.dlgOwner = ownerHwnd;

            ofn.filter = filterForApi;

            ofn.file = suggestedName + new String((char)0, 256 - suggestedName.Length);
            ofn.maxFile = ofn.file.Length;

            ofn.fileTitle = new String(new char[64]);
            ofn.maxFileTitle = ofn.fileTitle.Length;

            ofn.title = "Save";

            ofn.flags = 0x00000002;  // OFN_OVERWRITEPROMPT

            bool ok = LibWrap.GetSaveFileName(ofn);

            if (!ok)
            {
                int ext = LibWrap.CommDlgExtendedError();
                if (ext == 0)
                {
                    // User canceled
                    return null;
                }

                throw new InvalidOperationException($"GetSaveFileName failed with CommDlgExtendedError=0x{ext:X4}");
            }

            return ofn.file;
        }

        /// <summary>
        /// Tries to get the current MAUI window HWND for dialog ownership.
        /// </summary>
        private static IntPtr GetActiveWindowHandle()
        {
            try
            {
                var mauiWindow = Application.Current?.Windows?.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (mauiWindow is not null)
                {
                    return WinRT.Interop.WindowNative.GetWindowHandle(mauiWindow);
                }
            }
            catch
            {
                // Fallback to no owner
            }
            return IntPtr.Zero;
        }

        public Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken = default)
        {
            // This implementation ignores initialPath, as the dialog lets the user pick the location.
            return SaveAsync(fileName, stream, cancellationToken);
        }

        public Task<FileSaverResult> SaveAsync(string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            // Progress reporting is not supported in this implementation.
            return SaveAsync(fileName, stream, cancellationToken);
        }

        public Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            // Progress reporting and initialPath are not supported in this implementation.
            return SaveAsync(fileName, stream, cancellationToken);
        }


    }
}
#endif
