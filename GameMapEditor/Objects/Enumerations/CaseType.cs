﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMapEditor.Objects.Enumerations
{
    public enum CaseType
    {
        None = 0,
        Down = 1,
        Up = 2,
        Right = 4,
        Left = 8,
        Teleport = 16,
        Algo = 32
        // ...
    }
}
