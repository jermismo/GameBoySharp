﻿using GameBoySharp.Emu.Core;
using GameBoySharp.Emu.Utils;
using System.Diagnostics;

namespace GameBoySharp.Emu
{
    /// <summary>
    /// Emulator Host
    /// </summary>
    public class Emulator
    {
        private Task? runTask;

        /// <summary>
        /// The CPU
        /// </summary>
        public CPU CPU { get; private set; }

        /// <summary>
        /// The Memory Management Unit
        /// </summary>
        public MMU MMU { get; private set; }

        /// <summary>
        /// The Pixel Processing Unit
        /// </summary>
        public PPU PPU { get; private set; }

        /// <summary>
        /// The Audio Processing Unit
        /// </summary>
        public APU APU { get; private set; }

        /// <summary>
        /// The system timer
        /// </summary>
        public Timer Timer { get; private set; }

        /// <summary>
        /// The Joypad state controller.
        /// </summary>
        public Joypad Joypad { get; private set; }

        /// <summary>
        /// Shows if the enumlator is currently running.
        /// </summary>
        public bool PowerSwitch { get; private set; }

        /// <summary>
        /// Enable Sound Playback
        /// </summary>
        public bool SoundEnabled { get; set; }

        /// <summary>
        /// Locks the frame rate to the native rate (59.72 fps)
        /// </summary>
        public bool LockFrameRate { get; set; } = true;

        /// <summary>
        /// The current Frames Per Second
        /// </summary>
        public float FPS { get; private set; }

        public Emulator()
        {
            MMU = new MMU();
            PPU = new PPU();
            APU = new APU(MMU);

            Timer = new Timer();
            Joypad = new Joypad();

            CPU = new CPU(this);
        }

        /// <summary>
        /// Start the Emulator with a new ROM file.
        /// </summary>
        /// <param name="cartridgePath">The path to the Game Boy ROM.</param>
        public async void PowerOn(string cartridgePath)
        {
            if (PowerSwitch)
            {
                PowerOff();
            }
            if (runTask is not null) await runTask;

            CPU.Reset();
            PPU.Reset();
            APU.Reset();
            MMU.LoadGamePak(cartridgePath);

            PowerSwitch = true;

            runTask = Task.Factory.StartNew(Execute, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Stop the emulator.
        /// </summary>
        public void PowerOff()
        {
            PowerSwitch = false;
            FPS = 0;
        }

        /// <summary>
        /// Change the color pallete used to draw the screen.
        /// </summary>
        /// <param name="name">A name from <see cref="Emulator.Palettes"/></param>
        public void ChangePallete(string name)
        {
            if (Palettes.Any(p => p.Name == name))
            {
                PPU.Palette = Palettes.First(p => p.Name == name);
            }
        }

        /// <summary>
        /// Runs the emulator while the power is on.
        /// </summary>
        private void Execute()
        {
            var fpsWatch = new Stopwatch();
            var frameCounter = 0;
            
            fpsWatch.Start();

            while (PowerSwitch)
            {
                if (frameCounter > 30)
                {
                    FPS = (float)(frameCounter / fpsWatch.Elapsed.TotalSeconds);
                    fpsWatch.Restart();
                    frameCounter = 0;
                }

                CPU.RunCycles();
                frameCounter++;
            }
        }

        public static PaletteInfo[] Palettes = new PaletteInfo[] {
            new PaletteInfo("Default", new Color[] { new Color(0xFF), new Color(0xC0), new Color(0x60), new Color(0x00) }),
            new PaletteInfo("Classic Green", new Color[] { new Color(0x9C, 0xBA, 0x29), new Color(0x8C, 0xAB, 0x26), new Color(0x32, 0x61, 0x32), new Color(0x11, 0x37, 0x11) }),
            new PaletteInfo("Pocket", new Color[] { Color.FromInt(0xffe3e6c9), Color.FromInt(0xffc3c4a5), Color.FromInt(0xff8e8b61), Color.FromInt(0xff6c6c4e) }),
            new PaletteInfo("Yellow", new Color[] { Color.FromInt(0xfffff77b), Color.FromInt(0xffb5ae4a), Color.FromInt(0xff6b6931), Color.FromInt(0xff212010) }),
            new PaletteInfo("Soft", new Color[] { Color.FromInt(0xfff3f3f3), Color.FromInt(0xffa5a5a5), Color.FromInt(0xff525252), Color.FromInt(0xff262626) }),
            new PaletteInfo("Tan", new Color[] { Color.FromInt(0xeff7d79c), Color.FromInt(0xffb5a66b), Color.FromInt(0xff7b7163), Color.FromInt(0xff393829) }),
            new PaletteInfo("Orange", new Color[] { Color.FromInt(0xfff3f3f3), Color.FromInt(0xffffad63), Color.FromInt(0xff833100), Color.FromInt(0xff262626) }),
            new PaletteInfo("Lime", new Color[] { Color.FromInt(0xfff3f3f3), Color.FromInt(0xff7bff30), Color.FromInt(0xff008300), Color.FromInt(0xff262626) }),
            new PaletteInfo("Cherry", new Color[] { Color.FromInt(0xfff3f3f3), Color.FromInt(0xffff8584), Color.FromInt(0xff833100), Color.FromInt(0xff262626) }),
            new PaletteInfo("Sunset", new Color[] { Color.FromInt(0xfff3f3f3), Color.FromInt(0xfffe9494), Color.FromInt(0xff9394fe), Color.FromInt(0xff262626) }),
            new PaletteInfo("Seaside", new Color[] { Color.FromInt(0xfff3f3f3), Color.FromInt(0xff65a49b), Color.FromInt(0xff0000fe), Color.FromInt(0xff262626) }),
            new PaletteInfo("Watermelon", new Color[] { Color.FromInt(0xfff3f3f3), Color.FromInt(0xff51ff00), Color.FromInt(0xffff4200), Color.FromInt(0xff262626) }),
            new PaletteInfo("Salmon", new Color[] { Color.FromInt(0xfff3f3f3), Color.FromInt(0xffff8584), Color.FromInt(0xff943a3a), Color.FromInt(0xff262626) }),
            new PaletteInfo("Negative", new Color[] { Color.FromInt(0xff262626), Color.FromInt(0xff008486), Color.FromInt(0xffffde00), Color.FromInt(0xfff3f3f3) }),
            new PaletteInfo("Virtual Reality", new Color[] { Color.FromInt(0xff000000), Color.FromInt(0xff480000), Color.FromInt(0xff900000), Color.FromInt(0xfff00000) }),
        };
    }
}
