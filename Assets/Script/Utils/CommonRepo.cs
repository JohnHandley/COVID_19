using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonRepo : Singleton<CommonRepo>
{
    Vector2 inputVector = Vector2.zero;
    Vector2 rawInputVector = Vector2.zero;

    public Vector2 InputVector
    {
        get
        {
            return inputVector;
        }

        private set
        {
            inputVector = value;
        }
    }

    public Vector2 RawInputVector
    {
        get
        {
            return rawInputVector;
        }

        private set
        {
            rawInputVector = value;
        }
    }

    private void Update()
    {
        inputVector = new Vector2(Input.GetAxis(StaticStrings.Horizontal), Input.GetAxis(StaticStrings.Vertical));
        rawInputVector = new Vector2(Input.GetAxisRaw(StaticStrings.Horizontal), Input.GetAxisRaw(StaticStrings.Vertical));
    }
}
