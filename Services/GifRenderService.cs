using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ObjectRendering.Services
{
    public class GifRenderService
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private readonly GifBitmapEncoder _gEnc = new GifBitmapEncoder();

        public void Update(Graphics gfx, Rectangle size)
        {
            var bmpImage = new Bitmap(size.Width, size.Height, gfx);
            var bmp = bmpImage.GetHbitmap();
            var src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bmp,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            _gEnc.Frames.Add(BitmapFrame.Create(src));
            DeleteObject(bmp); // recommended, handle memory leak
        }

        public void Save()
        {
            var path = Path.Combine("output.gif", Directory.GetCurrentDirectory());
            using var fs = new FileStream(path, FileMode.Create);
            _gEnc.Save(fs);
        }
    }
}
