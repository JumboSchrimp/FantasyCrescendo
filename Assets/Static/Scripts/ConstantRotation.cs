﻿using UnityEngine;

public class ConstantRotation : MonoBehaviour {

    [SerializeField]
    private Vector3 rotationPerSecond;

    public Vector3 RotationPerSecond {
        get { return rotationPerSecond; }
        set { rotationPerSecond = value; }
    }


    // Update is called once per frame
	void Update () {
        transform.Rotate(RotationPerSecond * Util.dt);
	}
}
