﻿using System;

namespace XCOM2Launcher.Mod
{
    [Flags]
    public enum ModState
    {
        None = 0,
        New = 1,
        UpdateAvailable = 2,
        Downloading = 4,
        ModConflict = 8,
        DuplicateID = 16,
        NotLoaded = 32,
        NotInstalled = 64,
        DuplicatePrimary = 128,
        DuplicateDisabled = 256,
        MissingDependencies = 512,
    }
}