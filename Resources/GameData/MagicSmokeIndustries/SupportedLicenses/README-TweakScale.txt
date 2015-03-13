GoodspeedTweakScale
===================

Forked from Gaius Goodspeed's Goodspeed Aerospace Part & TweakScale plugin:
http://forum.kerbalspaceprogram.com/threads/72567-0-23-5-Goodspeed-Aerospace-Parts-TweakScale-plugin-v2014-4-1B

New Features:

Integration with Modular Fuel Tanks!
    Will automatically update MFT's volume and the volume of existing tanks.

Minimum and maximum scales!
    Want your part to be available only in 1.25m and 2.5m scales? Make it so!

Custom scale factors!
    Do you need 0.3125m scale in addition to the more common ones? Just add it! The maxScale and minScale automatically adapts to the number of scale factors.

Free scaling!
    Lets you create parts of any size. Great for trusses, lights, probably other stuff.

Solar panels!
    Solar panel performance now scales with the surface area of the panels.

More control over mass!
    For hollow, structural parts, mass probably scales with the surface area rather than the volume.

Engines!
    Yup, engines can now be rescaled, and the result will be somewhat roughly right. You might possibly get better results by scaling engines down than up.

Reaction Wheels!
    Parts with reaction wheels will now become more powerful when scaled up, and less powerful when scaled down.

KSP Interstellar parts!
    Correctly scales the physical properties as well as power output, waste heat, microwave transmission, etc. Currently supported parts:
    Solar Sails
    Microwave Receivers
    Atmospheric Scoops
    Atmospheric Intakes
    Heat Radiators
    Alcubierre Drives
    Engines (Not the Vista engine yet)
    Antimatter Storage Tanks
    Generators
    Engines
    Fusion Reactors
    As one can see, fission reactors, antimatter reactors and antimatter initiated reactors are not yet supported.
    


Example MODULE declarations:

MODULE
{
    name = GoodspeedTweakScale
    defaultScale = 2 // Default 1
    minScale = 1     // Default 0
    maxScale = 2     // Default 4
}

MODULE
{
    name = GoodspeedTweakScale
    freeScale = true    // Default false
    stepIncrement = 0.1 // Default 0.01 when freeScale is true
    minScale = 0.5      // Default 0.5 when freeScale is true
    maxScale = 2.0      // Default 4.0 when freeScale is true
}

MODULE
{
    name = GoodspeedTweakScale
    scaleFactors = 0.625, 1.25, 2.5, 3.75, 5.0 // Default range of scale factors
}

MODULE
{
    name = GoodspeedTweakScale
    massFactors = 0, 1, 0 // Scale with surface area, not volume
}


===================

This software is made available by the author under the terms of the
Creative Commons Attribution-NonCommercial-ShareAlike license.  See
the following web page for details:

http://creativecommons.org/licenses/by-nc-sa/4.0/

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.