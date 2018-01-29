using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Win32;

namespace snlib
{
    public static class WinFormsExtensions
    {
        public static void AppendLine( this TextBox source, string value )
        {
            if (source.Text.Length == 0)
                source.Text = value;
            else
                source.AppendText("\r\n" + value);
        }
    }

    public static class BooleanExtensions
    {
        public static string ToYesNoString( this bool value )
        {
            return value ? "Yes" : "No";
        }
    }

    public static class STime
    {

        public static DateTime GetLinkerTime( this Assembly assembly, TimeZoneInfo target = null )
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        private static Stopwatch sw = Stopwatch.StartNew();   
        public static long NowWatch()
        {
            long result = sw.Elapsed.Ticks;
            sw.Restart();
            return result;
        }

        private static int lasttick = Environment.TickCount;
        public static int NowTick()
        {
            return Environment.TickCount - lasttick;
        }
    }

    public static class SStruct
    {
        public static byte[] RawSerialize( object anything )
        {
            int rawsize = Marshal.SizeOf(anything);
            byte[] rawdata = new byte[rawsize];
            GCHandle handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);
            Marshal.StructureToPtr(anything, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return rawdata;
        }

        public static T ReadStruct<T>( FileStream fs )
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            fs.Read(buffer, 0, Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T temp = (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return temp;
        }
    }

    public static class SGraph
    {
        [DllImport("user32.dll")]
        static extern bool PrintWindow( IntPtr hWnd, IntPtr hdcBlt, int nFlags );

        public static Bitmap PrintWindow( IntPtr hWnd )
        {
            RECT rc = new RECT();
        
            User32.GetWindowRect(hWnd, ref rc);

            int width = rc.Right - rc.Left;

            int height = rc.Bottom - rc.Top;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            Graphics g = Graphics.FromImage(bmp);

            try {
                IntPtr hdc = g.GetHdc();

                PrintWindow(hWnd, hdc, 0);
                
            } finally {
                g.ReleaseHdc();

                g.Dispose();
            }

            return bmp;
        }

        public static void Cut( Bitmap Scrimg, Bitmap Destimg, int offset_x = 0, int offset_y = 0 )
        {
            //public static Rectangle PointsToRect( Point p1, Point p2 )
            //{
            //    return new Rectangle(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
            //}
            //var rect = new Rectangle(0, 0, Destimg.Width, Destimg.Height);
            //var path = new GraphicsPath();
            //path.AddRectangle(rect);
            using (var gr = Graphics.FromImage(Destimg))
            {
                //gr.SetClip(path, CombineMode.Intersect);
                gr.DrawImage(Scrimg, -offset_x, -offset_y);
            }
        }

        public static void BlachAndWhite( Bitmap bmpImg, Bitmap bmpDest, int P )
        {
            Color color = new Color();
            for (int j = 0; j < bmpImg.Height; j++)
            {
                for (int i = 0; i < bmpImg.Width; i++)
                {
                    color = bmpImg.GetPixel(i, j);
                    int K = ((color.R + color.G + color.B) / 3);
                    bmpDest.SetPixel(i, j, (K <= P ? Color.Black : Color.White));
                }
            }
        }

        public static int GetCountPixels( Bitmap img, int rgb )
        {
            Color color = new Color();
            int result = 0;
            for (int j = 0; j < img.Height; j++)
            {
                for (int i = 0; i < img.Width; i++)
                {
                    color = img.GetPixel(i, j);
                    if (rgb == color.ToArgb())
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        public static void MakeGray( Bitmap bmp )
        {
            PixelFormat pxf = PixelFormat.Format24bppRgb;

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            //Блокируем набор данных изображения в памяти
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, pxf);

            // Получаем адрес первой линии.
            IntPtr ptr = bmpData.Scan0;

            // Задаём массив из Byte и помещаем в него надор данных.
            // int numBytes = bmp.Width * bmp.Height * 3; 
            //На 3 умножаем - поскольку RGB цвет кодируется 3-мя байтами
            //Либо используем вместо Width - Stride
            int numBytes = bmpData.Stride * bmp.Height;
            int widthBytes = bmpData.Stride;
            byte[] rgbValues = new byte[numBytes];

            // Копируем значения в массив.
            Marshal.Copy(ptr, rgbValues, 0, numBytes);

            // Перебираем пикселы по 3 байта на каждый и меняем значения
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                int value = rgbValues[counter] + rgbValues[counter + 1] + rgbValues[counter + 2];
                byte color_b = 0;
                color_b = Convert.ToByte(value / 3);
                rgbValues[counter] = color_b;
                rgbValues[counter + 1] = color_b;
                rgbValues[counter + 2] = color_b;

            }
            // Копируем набор данных обратно в изображение
            Marshal.Copy(rgbValues, 0, ptr, numBytes);

            // Разблокируем набор данных изображения в памяти.
            bmp.UnlockBits(bmpData); // Получаем адрес первой линии.

        }

        public static Bitmap ResizeImg( Bitmap b, int nWidth, int nHeight )
        {
            Bitmap result = new Bitmap( nWidth, nHeight );
            using ( Graphics g = Graphics.FromImage( ( Image ) result ) )
            {
                g.InterpolationMode = InterpolationMode.Low; //качество обработки
                g.DrawImage(b, 0, 0, nWidth, nHeight);
                g.Dispose();
            }
            return result;
        }

        public static Bitmap Scr( string filename = "", int width = 0, int height = 0 )
        {
            if (width == 0 && height == 0) {

                width = Screen.PrimaryScreen.Bounds.Width;

                height = Screen.PrimaryScreen.Bounds.Height;

            }

            Bitmap bmp = new Bitmap( width, height );

            using (Graphics g = Graphics.FromImage( bmp )) {

                g.CopyFromScreen( 0, 0, 0, 0, bmp.Size );

                g.Dispose();

            }

            if (filename != "") bmp.Save( filename );

            return bmp;
        }

        public static Bitmap Scr( IntPtr hWnd, string filename = "" )
        {
            Bitmap result = PrintWindow(hWnd);
            if (filename != "") result.Save( filename );
            return result;
        }
    }

    public sealed class SGamma
    {
        #region win32
        [DllImport("user32.dll")]
        static extern IntPtr GetDC( IntPtr hWnd );

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("gdi32.dll")]
        static extern int GetDeviceGammaRamp( IntPtr hDC, ref RAMP lpRamp );

        [DllImport("gdi32.dll")]
        static extern int SetDeviceGammaRamp( IntPtr hDC, ref RAMP lpRamp );

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Blue;
        }
        #endregion
        public RAMP Ramp;
        public SGamma()
        {
            //Ramp = new RAMP();
            //Ramp.Blue = new ushort[ 256 ];
            //Ramp.Green = new ushort[ 256 ];
            //Ramp.Red = new ushort[ 256 ];
            GetGammaRamp();
        }
        ~SGamma()
        {
            SetDefGammaRamp();
        }

        public void SetGamma( int Value )
        {

            IntPtr DC = GetDC(GetDesktopWindow());

            if (DC != null)
            {

                RAMP _Rp = new RAMP();

                _Rp.Blue = new ushort[256];
                _Rp.Green = new ushort[256];
                _Rp.Red = new ushort[256];

                for (int i = 1; i < 256; i++)
                {
                    int value = i * (Value + 128);

                    if (value > 65535)
                        value = 65535;

                    _Rp.Red[i] = _Rp.Green[i] = _Rp.Blue[i] = Convert.ToUInt16(value);
                }

                SetDeviceGammaRamp(DC, ref _Rp);
            }
        }

        public static void SetGammaRamp( ref RAMP rmp )
        {
            IntPtr DC = GetDC(GetDesktopWindow());
            if (DC != null)
            {
                SetDeviceGammaRamp(DC, ref rmp);
            }
        }

        public static RAMP GetGammaRampFromMonitor()
        {
            IntPtr DC = GetDC(GetDesktopWindow());
            RAMP result = new RAMP();
            if (DC != null)
            {
                GetDeviceGammaRamp(DC, ref result);
            }
            return result;
        }

        private int GetGammaRamp()
        {
            IntPtr DC = GetDC(GetDesktopWindow());
            int res = 0;
            if (DC != null)
            {
                res = GetDeviceGammaRamp(DC, ref Ramp);
            }
            return res;
        }

        public int SetDefGammaRamp()
        {
            IntPtr DC = GetDC(GetDesktopWindow());
            int res = 0;
            if (DC != null)
            {
                res = SetDeviceGammaRamp(DC, ref Ramp);
            }
            return res;
        }
    }

    public static class SKeybd
    {
        public static void KeyPress( Keys key, int sleep = 50 )
        {
            KeyDown(key);
            Thread.Sleep(sleep);
            KeyUp(key);
        }

        public static void KeyDown( Keys key )
        {
            User32.keybd_event(key, 0, 1, 0);
        }

        public static void KeyUp( Keys key )
        {
            User32.keybd_event(key, 0, 2, 0);
        }

        public static void MouseMove( int dx, int dy )
        {
            User32.mouse_event(0x0001, dx, dy, 0, 0);
        }

        public static void LBClickEx2( int x = 0, int y = 0, bool restorepos = false,
            int do_click_sleep = 1, int click_sleep = 1, int after_click_sleep = 1 ) {

            POINT oldpos = new POINT();
            if (restorepos) User32.GetCursorPos( out oldpos );
            if (x != 0 && y != 0) User32.SetCursorPos( x, y );
            Thread.Sleep(do_click_sleep);
            User32.mouse_event( User32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0 );
            Thread.Sleep( click_sleep );
            User32.mouse_event( User32.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0 );
            if (restorepos) {
                Thread.Sleep( after_click_sleep );
                User32.SetCursorPos( oldpos.x, oldpos.y );
            }
        }
    }

    public static class SSndmsg
    {
        #region Native Mouse Clicks
        public static void LBDown() {
            User32.SendMessage((IntPtr)0, User32.WM_LBUTTONDOWN, 0, 0);
        }
        public static void LBUp() {
            User32.SendMessage((IntPtr)0, User32.WM_LBUTTONUP, 0, 0);
        }
        public static void RBDown() {
            User32.SendMessage((IntPtr)0, User32.WM_RBUTTONDOWN, 0, 0);
        }
        public static void RBUp() {
            User32.SendMessage((IntPtr)0, User32.WM_RBUTTONUP, 0, 0);
        }
        public static void LBClick(int sleep = 1) {
            LBDown(); Thread.Sleep(sleep); LBUp();
        }
        public static void RBClick(int sleep = 1) {
            RBDown(); Thread.Sleep(sleep); RBUp();
        }
        public static void LBDublClick(int sleep = 1, int dsleep = 1) {
            LBClick(sleep); Thread.Sleep(dsleep); LBClick(sleep);
        }
        public static void RBDublClick(int sleep = 1, int dsleep = 1) {
            RBClick(sleep); Thread.Sleep(dsleep); RBClick(sleep);
        }
        public static void LBClickEx(int x = 0, int y = 0, int sleep = 1, bool restorepos = false, int dorestoresleep = 1) {
            POINT oldpos = new POINT();
            if (restorepos) User32.GetCursorPos(out oldpos);
            if (x != 0 && y != 0) User32.SetCursorPos(x, y);
            LBDown(); Thread.Sleep(sleep); LBUp();
            if (restorepos) {
                Thread.Sleep(dorestoresleep);
                User32.SetCursorPos(oldpos.x, oldpos.y);
            }
        }
        #endregion
    }

    public static class SCommon
    {
        public static void ShakeForm( Form form )
        {
            var original = form.Location;
            var rnd = new Random(1337);
            const int shake_amplitude = 10;
            for (int i = 0; i < 10; i++)
            {
                form.Location = new Point(original.X + rnd.Next(-shake_amplitude, shake_amplitude), original.Y + rnd.Next(-shake_amplitude, shake_amplitude));
                System.Threading.Thread.Sleep(20);
            }
            form.Location = original;
        }

        #region Windows Api
        public static string GetWindowText(IntPtr hWnd)
        {
            int len = User32.GetWindowTextLength(hWnd) + 1;
            StringBuilder sb = new StringBuilder(len);
            len = User32.GetWindowText(hWnd, sb, len);
            return sb.ToString(0, len);
        }
        #endregion
    }

    public static class SPath {

        public static string Desctop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\";

    }

    

}
