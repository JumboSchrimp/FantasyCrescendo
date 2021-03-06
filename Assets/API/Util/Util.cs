﻿using UnityEngine;
using System.Collections;

public class Util : MonoBehaviour {

    public const float dt = 1/60f;

    public static int Sign(int value) {
        if (value > 0)
            return 1;
        if (value < 0)
            return -1;
        return 0;
    }

    public static int MatchSign(int src, int sign) {
        return (Sign(src) != Sign(sign)) ? -src : src;
    }

    public static float MatchSign(float src, float sign) {
        return (Sign((int)src) != Sign((int)sign)) ? -src : src;
    }
}
