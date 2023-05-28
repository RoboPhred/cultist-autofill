# Cultist Autofill

This is a mod for Cultist Simulator that adds a hotkey to automatically fill empty slots in a situation / verb.

With a situation window open, press `R`. This will cause the mod to look at each empty slot, move in any matching cards it can find. Cards will be searched based on
their closeness to the situation/verb block on the table. Closer cards will be prioritized.

## Development

### Dependencies

Project dependencies should be placed in a folder called `externals` in the project's root directory.
This folder should include all dependencies found in the csproj file.

### Compiling

This project uses the dotnet cli, provided by the .Net SDK. To compile, simply use `dotnet build` on the project's root directory.

## Inspiration

This mod is inspired by two [ililim mods](https://github.com/ililim/mods-cultist-simulator), specifically ShiftPopulate and Situation Automation. As these mods are no longer mantained, this one may yet grow to cover their feature set.
