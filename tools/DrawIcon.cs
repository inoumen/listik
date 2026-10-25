using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// Original Listik artwork, drawn as paths so each icon size stays crisp.
class DrawIcon
{
    static Bitmap Draw(int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
            using (var body = new SolidBrush(Color.FromArgb(255, 211, 78)))
                using (var fold = new SolidBrush(Color.FromArgb(255, 244, 182)))
                    using (var ink = new Pen(Color.FromArgb(166, 119, 37), 1.6f))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.ScaleTransform(size / 32f, size / 32f);
                        ink.StartCap = ink.EndCap = LineCap.Round;
                        using (var shape = new GraphicsPath())
                        {
                            shape.AddArc(3, 3, 8, 8, 180, 90);
                            shape.AddLine(7, 3, 21, 3);
                            shape.AddLine(21, 3, 28, 10);
                            shape.AddLine(28, 10, 28, 24);
                            shape.AddArc(20, 20, 8, 8, 0, 90);
                            shape.AddArc(3, 20, 8, 8, 90, 90);
                            shape.CloseFigure();
                            g.FillPath(body, shape);
                        }

                        g.FillPolygon(fold, new[]{new PointF(21, 3), new PointF(21, 10), new PointF(28, 10)});
                        g.DrawLine(ink, 9, 14, 22, 14);
                        g.DrawLine(ink, 9, 19, 22, 19);
                        g.DrawLine(ink, 9, 24, 17, 24);
                    }

        return bitmap;
    }

    static void Main(string[] args)
    {
        int[] sizes = {16, 20, 24, 32, 48, 64, 128, 256};
        var frames = new byte[sizes.Length][];
        for (int i = 0; i < sizes.Length; i++)
            using (var bitmap = Draw(sizes[i]))
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    frames[i] = stream.ToArray();
                }

        using (var writer = new BinaryWriter(File.Create(args[0])))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(frames[i].Length);
                writer.Write(offset);
                offset += frames[i].Length;
            }

            foreach (var frame in frames)
                writer.Write(frame);
        }

        using (var preview = Draw(256))
            preview.Save(args[1], ImageFormat.Png);
    }
}
