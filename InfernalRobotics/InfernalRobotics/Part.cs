using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MuMechPart : Part
{
    private static int s_creationOrder = 0;
    public int creationOrder = 0;

    public static string traceTrans(string prev, Transform tr, CelestialBody body = null)
    {
        string tmp;
        if (body != null)
        {
            tmp = prev + "." + tr.name + " - (" + body.GetLatitude(tr.position) + ", " + body.GetLongitude(tr.position) + ", " + body.GetAltitude(tr.position) + ")\n";
        }
        else
        {
            tmp = prev + "." + tr.name + " - (" + tr.position + ", " + tr.rotation + ")\n";
        }
        Component[] comps = tr.gameObject.GetComponents<Component>();
        foreach (Component comp in comps)
        {
            tmp += "\t" + comp.GetType().Name + " - " + comp.name + "\n";
        }
        for (int i = 0; i < tr.childCount; i++)
        {
            tmp += traceTrans(prev + "." + tr.name, tr.GetChild(i), body);
        }
        return tmp;
    }

    public bool isSymmMaster()
    {
        for (int i = 0; i < symmetryCounterparts.Count; i++)
        {
            if (symmetryCounterparts[i].GetComponent<MuMechPart>().creationOrder < creationOrder)
            {
                return false;
            }
        }
        return true;
    }

    protected override void onPartStart()
    {
        base.onPartStart();
        creationOrder = s_creationOrder++;
    }
}
