using GameBoySharp.Emu;
using NAudio.Wave;
using System.ComponentModel;

namespace GameBoySharpWinForms
{
    public partial class MainForm : Form
    {

        private Emulator emulator;
        private WaveOutEvent outputDevice;
        private ApuBufferReader? soundProvider;
        private bool isDrawing = false;
        private Bitmap bitmap = new(160, 144, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        public MainForm()
        {
            emulator = new Emulator();
            emulator.AudioEnabled = true;
            outputDevice = new WaveOutEvent();

            InitializeComponent();

            pictureBox1.Image = bitmap;

            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;

            emulator.PPU.FrameReady += PPU_FrameReady;

            if (emulator.APU is not null)
            {
                soundProvider = new ApuBufferReader(emulator.APU);
                outputDevice.Init(soundProvider);
                outputDevice.Play();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            emulator.PPU.FrameReady -= PPU_FrameReady;
            base.OnClosing(e);
        }

        #region Keyboard

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            HandleKey(e.KeyCode, true);
        }

        private void Form1_KeyUp(object? sender, KeyEventArgs e)
        {
            HandleKey(e.KeyCode, false);
        }

        private void HandleKey(Keys key, bool isDown)
        {
            switch (key)
            {
                case Keys.A:
                case Keys.Left:
                    emulator.Joypad.Left(isDown);
                    break;
                case Keys.W:
                case Keys.Up:
                    emulator.Joypad.Up(isDown);
                    break;
                case Keys.D:
                case Keys.Right:
                    emulator.Joypad.Right(isDown);
                    break;
                case Keys.S:
                case Keys.Down:
                    emulator.Joypad.Down(isDown);
                    break;
                case Keys.Oemcomma:
                case Keys.D0:
                    emulator.Joypad.AButton(isDown);
                    break;
                case Keys.OemPeriod:
                    emulator.Joypad.BButton(isDown);
                    break;
                case Keys.Enter:
                    emulator.Joypad.StartButton(isDown);
                    break;
                case Keys.OemQuotes:
                    emulator.Joypad.SelectButton(isDown);
                    break;
            }
        }

        #endregion

        private void PPU_FrameReady(object? sender, EventArgs e)
        {
            pictureBox1.BeginInvoke(new Action(DrawPicture));
        }

        private void DrawPicture()
        {
            if (isDrawing) return;
            isDrawing = true;

            var bmp = bitmap.LockBits(new Rectangle(0, 0, 160, 144), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            unsafe
            {
                var scan = new Span<byte>(bmp.Scan0.ToPointer(), bmp.Stride * bmp.Height * 4);
                var raw = new Span<byte>(emulator.PPU.Bitmap.RawData);
                raw.CopyTo(scan);
            }

            bitmap.UnlockBits(bmp);

            pictureBox1.Invalidate();
            isDrawing = false;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Game Boy Files | *.gb";
            dlg.AddToRecent = true;
            
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(dlg.FileName))
                {
                    emulator.PowerOff();
                    emulator.PowerOn(dlg.FileName);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}