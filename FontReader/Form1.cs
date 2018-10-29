using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace FontReader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            TestRun();
        }

        private void TestRun()
        {
            if (outputPictureBox.Image != null) {
                outputPictureBox.Image.Dispose();
            }
            var img = new Bitmap(640, 480, PixelFormat.Format24bppRgb);

            for (int y = 0; y < 480; y++)
            {
                for (int x = 0; x < 640; x++)
                {
                    img.SetPixel(x, y, Color.FromArgb(x % 128, y % 255, 128 ));
                }
            }


            outputPictureBox.Image = img;
            Width = img.Width + 18;
            Height = img.Height + 41;
        }
    }
}
