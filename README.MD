# GameBoy Sharp
GameBoy Sharp is another GameBoy (DMG) emulator written in C# (and .NET 7). 

This is a *personal project* written primarily with a focus on readability, and then looking at performance improvements.

Most of this emulator is based on [Project DMG](https://github.com/BluestormDNA/ProjectDMG) and [Retr.io GB](https://github.com/retrio/gb).

# Compatibility
This emulator has been manually tested with a handful of popular games and appeared to be compatible. The emulators it is based on have ~95% compatiblity.

# Known Issues
- Sound is not currently working, but most of what is needed should be in place.
- The memory and interrupt timings are not accurrate, but are good enough to run most games

# Controls
- **D-PAD:** WASD or Arrow Keys
- **A Button:** ',' or '0'
- **B BUtton:** '.'
- **Start**: Enter
- **Select:** ' (single quote)

# Solution Layout
- **GameBoySharp.Emu** - cross-platform emulator core written in C#
- **GameBoySharp.Emy.Test** - unit test project
- **GameBoySharpWinForms** - a very simple Windows Forms host for the emulator

# Test ROMs
Compiled versions of the Blargg and Mooneye test ROMs are included in the Tests project. Right now only the Blargg tests are hooked up as Unit Tests.

## Blargg Test Results

**CPU Tests**

|Test|Status|
|---|---|
|01-special|Passed|
|02-interrupts|Passed|
|03-op sp,hl|Passed|
|04-op r,imm|Passed|
|05-op rp|Passed|
|06-ld r,r|Passed|
|07-jr,jp,call,ret,rst|Passed|
|08-misc instrs|Passed|
|09-op r,r|Passed|
|10-bit ops|Passed|
|11-op a,(h1)|Passed|

**Memory Timing**
*Note* These don't work correcly in the unit tests.

|Test|Status|
|---|---|
|01-read_timing|Failed|
|02-write_timing|Failed|
|03-modify_timing|Failed|

**Other Tests**

|Test|Status|
|---|---|
|halt_bug|Passed|
|instr_timing|Failed|
|interrupt_time|Failed|
|oam_bug|Failed|

# Other Resources
- [Gameboy Development blog](https://gbdev.gg8.se/)
- [Game Boy Development community](https://gbdev.io/)
