using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Silo
{
    public string userID;
    public string siloName;
    public string siloLength;
    public bool floor;

    public Silo(string userID, string siloName, string siloLength, bool floor)
    {
        this.userID = userID;
        this.siloName = siloName;
        this.siloLength = siloLength;
        this.floor = floor;
    }
}
