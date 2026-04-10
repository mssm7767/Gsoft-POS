using System;
using System.Runtime.InteropServices;

namespace GSoftPosNew.Services.Printing
{
    public class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendStringToPrinter(string printerName, string text)
        {
            IntPtr hPrinter;
            DOCINFOA di = new DOCINFOA
            {
                pDocName = "GSoft POS Receipt",
                pDataType = "RAW"
            };

            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                return false;

            try
            {
                if (!StartDocPrinter(hPrinter, 1, di))
                    return false;

                if (!StartPagePrinter(hPrinter))
                    return false;

                IntPtr pBytes = Marshal.StringToCoTaskMemAnsi(text);

                try
                {
                    int dwWritten;
                    bool success = WritePrinter(hPrinter, pBytes, text.Length, out dwWritten);
                    return success;
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pBytes);
                }
            }
            finally
            {
                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);
            }
        }

        public static bool SendBytesToPrinter(string printerName, byte[] bytes)
        {
            IntPtr hPrinter;
            DOCINFOA di = new DOCINFOA
            {
                pDocName = "GSoft POS Receipt Cut",
                pDataType = "RAW"
            };

            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                return false;

            try
            {
                if (!StartDocPrinter(hPrinter, 1, di))
                    return false;

                if (!StartPagePrinter(hPrinter))
                    return false;

                IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);

                try
                {
                    Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);

                    int dwWritten;
                    return WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out dwWritten);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pUnmanagedBytes);
                }
            }
            finally
            {
                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);
            }
        }
    }
}