using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Imaging;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace SBR_Game.Rendering
{
    public class Texture2D : IDisposable
    {
        public int Handle { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private bool _disposed;

        private Texture2D() { }

        public static Texture2D LoadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Texture file not found: {path}");

            using var image = new Bitmap(path);
            return LoadFromBitmap(image);
        }

        private static Texture2D LoadFromBitmap(Bitmap bitmap)
        {
            int handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, handle);

            using var converted = new Bitmap(bitmap.Width, bitmap.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(converted))
                g.DrawImage(bitmap, 0, 0);

            var data = converted.LockBits(
                new Rectangle(0, 0, converted.Width, converted.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int width = converted.Width;
            int height = converted.Height;
            int expectedStride = width * 4;

            byte[] pixels = new byte[width * height * 4];
            unsafe
            {
                byte* src = (byte*)data.Scan0;
                for (int y = 0; y < height; y++)
                    System.Runtime.InteropServices.Marshal.Copy(
                        (IntPtr)(src + y * data.Stride),
                        pixels,
                        (height - 1 - y) * expectedStride,
                        expectedStride);
            }
            converted.UnlockBits(data);

            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            return new Texture2D { Handle = handle, Width = bitmap.Width, Height = bitmap.Height };
        }

        public void Use() => GL.BindTexture(TextureTarget.Texture2D, Handle);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GL.DeleteTexture(Handle);
            Handle = 0;
        }
    }
}