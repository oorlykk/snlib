using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace xMonitor
{
    sealed class Gamma
    {
        #region win32
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("gdi32.dll")]
        static extern int GetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport("gdi32.dll")]
        static extern int SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

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
        public Gamma() {
            //Ramp = new RAMP();
            //Ramp.Blue = new ushort[ 256 ];
            //Ramp.Green = new ushort[ 256 ];
            //Ramp.Red = new ushort[ 256 ];
            GetGammaRamp();
        }
        ~Gamma() {
            SetDefGammaRamp();
        }

        public void SetGamma(int Value)
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

        public static void SetGammaRamp(ref RAMP rmp )
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

        private int GetGammaRamp() {
            IntPtr DC = GetDC( GetDesktopWindow() );
            int res = 0;
            if ( DC != null ) {
                res = GetDeviceGammaRamp( DC, ref Ramp );
            }
            return res;
        }

        public int SetDefGammaRamp() {
            IntPtr DC = GetDC( GetDesktopWindow() );
            int res = 0;
            if ( DC != null ) {
                res = SetDeviceGammaRamp( DC, ref Ramp );
            }
            return res;
        }
    }
}